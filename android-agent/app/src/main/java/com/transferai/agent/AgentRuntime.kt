package com.transferai.agent

import java.security.SecureRandom
import java.util.concurrent.CountDownLatch
import java.util.concurrent.TimeUnit

object AgentRuntime {
    @Volatile var running: Boolean = false
    @Volatile var pairingCode: String? = null
    @Volatile var status: String = "الوكيل متوقف"
    @Volatile var lastPeer: String = ""
    @Volatile var verificationCode: String? = null
    @Volatile var localVerificationConfirmed: Boolean = false
    @Volatile private var verificationLatch: CountDownLatch = CountDownLatch(0)

    fun beginSession() {
        verificationCode = null
        localVerificationConfirmed = false
        verificationLatch = CountDownLatch(1)
    }

    fun confirmLocalVerification(): Boolean {
        if (verificationCode.isNullOrBlank()) return false
        localVerificationConfirmed = true
        verificationLatch.countDown()
        return true
    }

    fun awaitLocalVerification(timeout: Long, unit: TimeUnit): Boolean =
        localVerificationConfirmed || verificationLatch.await(timeout, unit)

    fun endSession() {
        verificationCode = null
        localVerificationConfirmed = false
        verificationLatch.countDown()
    }

    fun newPairingCode(): String {
        val value = SecureRandom().nextInt(100_000_000)
        return value.toString().padStart(8, '0')
    }

    fun newCertificateId(): String =
        ByteArray(8).also { SecureRandom().nextBytes(it) }
            .joinToString("") { "%02x".format(it) }
}
