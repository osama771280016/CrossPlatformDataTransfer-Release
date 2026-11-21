package com.transferai.agent

import android.os.Build
import org.json.JSONObject
import java.io.BufferedInputStream
import java.io.BufferedOutputStream
import java.io.DataInputStream
import java.io.DataOutputStream
import java.net.Socket
import java.security.MessageDigest

object AgentProtocol {
    const val VERSION = 1
    private const val MAX_FRAME_BYTES = 1024 * 1024
    private const val MAX_BAD_PAIRING_ATTEMPTS = 5

    fun serve(socket: Socket, pairingCode: String) {
        DataInputStream(BufferedInputStream(socket.getInputStream())).use { input ->
            DataOutputStream(BufferedOutputStream(socket.getOutputStream())).use { output ->
                var paired = false
                var badPairingAttempts = 0

                while (!socket.isClosed) {
                    val frameSize = try {
                        input.readInt()
                    } catch (_: java.io.EOFException) {
                        return
                    }

                    if (frameSize !in 1..MAX_FRAME_BYTES) {
                        writeResponse(output, response(false, "حجم رسالة غير صالح"))
                        return
                    }

                    val frame = ByteArray(frameSize)
                    input.readFully(frame)
                    val request = try {
                        JSONObject(String(frame, Charsets.UTF_8))
                    } catch (_: Exception) {
                        writeResponse(output, response(false, "صيغة الطلب غير صالحة"))
                        continue
                    }

                    val command = request.optString("commandType", "").uppercase()
                    val parameters = request.optJSONObject("parameters") ?: JSONObject()
                    val result = when {
                        command == "PAIR" -> {
                            val supplied = parameters.optString("pairingCode", "")
                            val matches = MessageDigest.isEqual(
                                supplied.toByteArray(Charsets.UTF_8),
                                pairingCode.toByteArray(Charsets.UTF_8)
                            )
                            if (matches) {
                                paired = true
                                response(true, "تم اقتران هذه الجلسة مؤقتًا")
                            } else {
                                badPairingAttempts++
                                try {
                                    Thread.sleep(1000)
                                } catch (_: InterruptedException) {
                                    Thread.currentThread().interrupt()
                                    return
                                }
                                response(false, "رمز الاقتران غير صحيح")
                            }
}
                        !paired -> response(false, "يجب إتمام الاقتران قبل إرسال الأوامر")
                        command == "PING" -> response(true, "PONG")
                        command == "HANDSHAKE" -> response(
                            true,
                            JSONObject()
                                .put("protocolVersion", VERSION)
                                .put("deviceId", Build.MODEL ?: "android-device")
                                .put("model", Build.MODEL ?: "unknown")
                                .put("androidApi", Build.VERSION.SDK_INT)
                                .toString()
                        )
                        command == "GET_CAPABILITIES" -> response(
                            true,
                            JSONObject()
                                .put("protocolVersion", VERSION)
                                .put("read", org.json.JSONArray())
                                .put("write", org.json.JSONArray())
                                .put("note", "لا يقرأ هذا الإصدار بيانات الهاتف أو يكتبها")
                                .toString()
                        )
                        else -> response(false, "الأمر غير مدعوم في إصدار الوكيل الأول")
                    }

                    writeResponse(output, result)
                    if (command == "PAIR" && !paired && badPairingAttempts >= MAX_BAD_PAIRING_ATTEMPTS) {
                        return
                    }
                }
            }
        }
    }

    private fun response(success: Boolean, message: String) = JSONObject()
        .put("success", success)
        .put("message", message)
        .put("data", JSONObject.NULL)

    private fun writeResponse(output: DataOutputStream, response: JSONObject) {
        val bytes = response.toString().toByteArray(Charsets.UTF_8)
        output.writeInt(bytes.size) // DataOutputStream writes the 4-byte length in big-endian order.
        output.write(bytes)
        output.flush()
    }

}
