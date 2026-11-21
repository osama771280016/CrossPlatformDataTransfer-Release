package com.transferai.agent

import android.Manifest
import android.app.Activity
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.Gravity
import android.view.ViewGroup
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView

class MainActivity : Activity() {
    private lateinit var statusView: TextView
    private lateinit var codeView: TextView
    private lateinit var verificationView: TextView
    private lateinit var confirmVerificationButton: Button
    private val handler = Handler(Looper.getMainLooper())
    private var startAfterNotificationPermission = false

    private val refresh = object : Runnable {
        override fun run() {
            if (!::statusView.isInitialized) return
            statusView.text = AgentRuntime.status
            codeView.text = AgentRuntime.pairingCode?.let { "رمز الاقتران المؤقت\n$it" } ?: "لا يوجد رمز اقتران نشط"
            verificationView.text = AgentRuntime.verificationCode?.let {
                "رمز تحقق القناة — قارنه مع Windows\n$it"
            } ?: "سيظهر رمز تحقق القناة بعد بدء الوكيل"
            confirmVerificationButton.isEnabled =
                AgentRuntime.verificationCode != null && !AgentRuntime.localVerificationConfirmed
            if (AgentRuntime.localVerificationConfirmed) {
                confirmVerificationButton.text = "تم تأكيد تطابق الرمز"
            } else {
                confirmVerificationButton.text = "تأكيد أن الرمز يطابق Windows"
            }
            handler.postDelayed(this, 1000)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val layout = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER_HORIZONTAL
            setPadding(32, 40, 32, 24)
        }
        val title = TextView(this).apply {
            text = "TransferAI Agent"
            textSize = 26f
            gravity = Gravity.CENTER
        }
        statusView = TextView(this).apply {
            textSize = 17f
            gravity = Gravity.CENTER
            setPadding(0, 28, 0, 12)
        }
        codeView = TextView(this).apply {
            textSize = 23f
            gravity = Gravity.CENTER
            setPadding(0, 12, 0, 24)
        }
        verificationView = TextView(this).apply {
            textSize = 18f
            gravity = Gravity.CENTER
            setPadding(0, 8, 0, 12)
        }
        val startButton = Button(this).apply {
            text = "بدء الاتصال"
            setOnClickListener { requestNotificationPermissionOrStart() }
        }
        val stopButton = Button(this).apply {
            text = "إيقاف الاتصال"
            setOnClickListener { stopAgent() }
        }
        confirmVerificationButton = Button(this).apply {
            text = "تأكيد أن الرمز يطابق Windows"
            isEnabled = false
            setOnClickListener {
                if (AgentRuntime.confirmLocalVerification()) {
                    AgentRuntime.status = "تأكد المستخدم من تطابق رمز تحقق TLS"
                }
            }
        }
        val info = TextView(this).apply {
            text = "يُعرض رمز تحقق للقناة الآمنة؛ قارنه مع Windows وأكّده على الطرفين. لا تقرأ هذه النسخة ملفات أو صورًا أو رسائل ولا تنقل البيانات بعد."
            textSize = 15f
            gravity = Gravity.CENTER
            setPadding(0, 24, 0, 0)
        }

        listOf(title, statusView, codeView, verificationView, confirmVerificationButton, startButton, stopButton, info).forEach { view ->
            layout.addView(view, LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))
        }
        setContentView(layout)
        handler.post(refresh)
    }

    private fun requestNotificationPermissionOrStart() {
        if (Build.VERSION.SDK_INT >= 33 &&
            checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED
        ) {
            startAfterNotificationPermission = true
            requestPermissions(arrayOf(Manifest.permission.POST_NOTIFICATIONS), REQUEST_NOTIFICATIONS)
            return
        }
        startAgent()
    }

    override fun onRequestPermissionsResult(requestCode: Int, permissions: Array<out String>, grantResults: IntArray) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode == REQUEST_NOTIFICATIONS && startAfterNotificationPermission) {
            startAfterNotificationPermission = false
            // The foreground-service notification is important for transparency. Respect denial.
            if (grantResults.firstOrNull() == PackageManager.PERMISSION_GRANTED) startAgent()
            else AgentRuntime.status = "امنح إذن الإشعارات لبدء خدمة الاتصال وإظهار حالتها"
        }
    }

    private fun startAgent() {
        val code = AgentRuntime.newPairingCode()
        AgentRuntime.pairingCode = code
        AgentRuntime.status = "جارٍ تشغيل خدمة الاتصال..."
        val intent = Intent(this, AgentForegroundService::class.java)
            .setAction(AgentForegroundService.ACTION_START)
            .putExtra(AgentForegroundService.EXTRA_PAIRING_CODE, code)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) startForegroundService(intent) else startService(intent)
    }

    private fun stopAgent() {
        stopService(Intent(this, AgentForegroundService::class.java).setAction(AgentForegroundService.ACTION_STOP))
        AgentRuntime.pairingCode = null
        AgentRuntime.endSession()
        AgentRuntime.status = "الوكيل متوقف"
    }

    override fun onDestroy() {
        handler.removeCallbacks(refresh)
        super.onDestroy()
    }

    companion object {
        private const val REQUEST_NOTIFICATIONS = 4712
    }
}
