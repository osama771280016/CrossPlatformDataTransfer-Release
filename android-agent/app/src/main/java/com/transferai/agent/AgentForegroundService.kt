package com.transferai.agent

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import java.net.ServerSocket
import java.net.Socket
import java.util.concurrent.TimeUnit
import javax.net.ssl.SSLSocket

class AgentForegroundService : Service() {
    private var serverSocket: ServerSocket? = null
    private var listenerThread: Thread? = null
    @Volatile private var currentPeer: Socket? = null
    @Volatile private var stopping = false

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            stopAgent()
            stopSelf(startId)
            return START_NOT_STICKY
        }

        val code = intent?.getStringExtra(EXTRA_PAIRING_CODE)
        if (code.isNullOrBlank()) {
            AgentRuntime.status = "تعذر البدء: رمز الاقتران غير متوفر"
            stopSelf(startId)
            return START_NOT_STICKY
        }

        AgentRuntime.pairingCode = code
        createNotificationChannel()
        val notification = buildNotification()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_DATA_SYNC)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }

        if (listenerThread?.isAlive != true) {
            AgentRuntime.beginSession()
            startListener()
        }
        return START_NOT_STICKY
    }

    private fun startListener() {
        stopping = false
        listenerThread = Thread({
            try {
                val socket = AgentTls.createServerSocket(PORT)
                serverSocket = socket
                AgentRuntime.running = true
                AgentRuntime.status = "بانتظار اتصال USB/ADB على المنفذ $PORT"

                while (!socket.isClosed) {
                    val peer = socket.accept() as SSLSocket
                    currentPeer = peer
                    AgentRuntime.lastPeer = "اتصال محلي"
                    AgentRuntime.status = "اتصال TLS؛ قارن رمز التحقق في الهاتف وWindows"
                    try {
                        val currentCode = AgentRuntime.pairingCode
                        if (currentCode == null) {
                            peer.close()
                        } else {
                            peer.use {
                                it.soTimeout = 120_000
                                it.startHandshake()
                                val approved = AgentRuntime.awaitLocalVerification(120, TimeUnit.SECONDS)
                                if (approved) {
                                    AgentRuntime.status = "تم تأكيد رمز TLS؛ بانتظار رمز الاقتران من Windows"
                                    AgentProtocol.serve(it, currentCode)
                                } else {
                                    AgentRuntime.status = "انتهت مهلة تأكيد رمز TLS؛ أعد تشغيل الوكيل"
                                }
                            }
                        }
                    } catch (error: Exception) {
                        AgentRuntime.status = "انقطع الاتصال: ${error.javaClass.simpleName}"
                    } finally {
                        currentPeer = null
                    }
                    AgentRuntime.status = "انتهت جلسة الاتصال؛ أعد تشغيل الوكيل لاتصال جديد"
                    break
                }
            } catch (error: Exception) {
                if (!stopping) {
                    AgentRuntime.status = "تعذر فتح المنفذ $PORT: ${error.message ?: error.javaClass.simpleName}"
                }
            } finally {
                AgentRuntime.running = false
                if (stopping || AgentRuntime.status.startsWith("بانتظار")) AgentRuntime.status = "الوكيل متوقف"
                try {
                    serverSocket?.close()
                } catch (_: Exception) {
                }
                serverSocket = null
                AgentTls.clearSessionKey()
                stopSelf()
            }
        }, "android-agent-listener").apply {
            isDaemon = true
            start()
        }
    }

    private fun stopAgent() {
        stopping = true
        AgentRuntime.running = false
        AgentRuntime.pairingCode = null
        AgentRuntime.endSession()
        AgentTls.clearSessionKey()
        AgentRuntime.status = "الوكيل متوقف"
        try {
            serverSocket?.close()
        } catch (_: Exception) {
        }
        try {
            currentPeer?.close()
        } catch (_: Exception) {
        }
        serverSocket = null
        currentPeer = null
        listenerThread = null
        stopForeground(STOP_FOREGROUND_REMOVE)
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "اتصال نقل البيانات", NotificationManager.IMPORTANCE_LOW)
            )
        }
    }

    private fun buildNotification(): Notification {
        val stopIntent = Intent(this, AgentForegroundService::class.java).setAction(ACTION_STOP)
        val pendingIntent = PendingIntent.getService(
            this,
            1,
            stopIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        val builder = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            Notification.Builder(this, CHANNEL_ID)
        } else {
            @Suppress("DEPRECATION")
            Notification.Builder(this)
        }
        return builder
            .setSmallIcon(android.R.drawable.stat_sys_upload)
            .setContentTitle("TransferAI Agent")
            .setContentText("خدمة الاتصال المحلي نشطة حتى توقفها")
            .setOngoing(true)
            .addAction(android.R.drawable.ic_media_pause, "إيقاف", pendingIntent)
            .build()
    }

    override fun onTimeout(startId: Int, fgsType: Int) {
        stopAgent()
        stopSelf(startId)
    }

    override fun onDestroy() {
        stopAgent()
        super.onDestroy()
    }

    companion object {
        const val ACTION_START = "com.transferai.agent.action.START"
        const val ACTION_STOP = "com.transferai.agent.action.STOP"
        const val EXTRA_PAIRING_CODE = "pairingCode"
        const val PORT = 4711
        private const val CHANNEL_ID = "transfer_agent"
        private const val NOTIFICATION_ID = 4711
    }
}
