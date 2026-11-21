package com.transferai.agent

import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import java.math.BigInteger
import java.net.InetAddress
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.SecureRandom
import java.security.spec.ECGenParameterSpec
import java.util.Date
import java.util.Collections
import javax.net.ssl.KeyManagerFactory
import javax.net.ssl.SSLContext
import javax.net.ssl.SSLServerSocket
import javax.security.auth.x500.X500Principal

object AgentTls {
    private const val ALIAS_PREFIX = "transferai.agent.session."
    @Volatile private var activeAlias: String? = null

    fun createServerSocket(port: Int): SSLServerSocket {
        val keyStore = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        Collections.list(keyStore.aliases())
            .filter { it.startsWith(ALIAS_PREFIX) }
            .forEach { keyStore.deleteEntry(it) }

        val alias = ALIAS_PREFIX + AgentRuntime.newCertificateId()
        val now = System.currentTimeMillis()
        val serialBytes = ByteArray(16).also { SecureRandom().nextBytes(it) }
        val specification = KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_SIGN)
            .setAlgorithmParameterSpec(ECGenParameterSpec("secp256r1"))
            .setDigests(KeyProperties.DIGEST_SHA256)
            .setCertificateSubject(X500Principal("CN=TransferAI Agent"))
            .setCertificateSerialNumber(BigInteger(1, serialBytes).add(BigInteger.ONE))
            .setCertificateNotBefore(Date(now - 60_000L))
            .setCertificateNotAfter(Date(now + 6 * 60 * 60 * 1000L))
            .build()

        KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_EC, "AndroidKeyStore").apply {
            initialize(specification)
            generateKeyPair()
        }
        activeAlias = alias
        val refreshedKeyStore = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        val certificate = refreshedKeyStore.getCertificate(alias)
            ?: throw IllegalStateException("Android Keystore did not create the session certificate")
        AgentRuntime.verificationCode = verificationCode(certificate.encoded)

        val keyManagers = KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm()).apply {
            init(refreshedKeyStore, null)
        }.keyManagers
        val context = SSLContext.getInstance("TLS").apply {
            init(keyManagers, null, SecureRandom())
        }
        val server = context.serverSocketFactory.createServerSocket(
            port,
            1,
            InetAddress.getByName("127.0.0.1")
        ) as SSLServerSocket
        val supported = server.supportedProtocols.toSet()
        val enabled = listOf("TLSv1.3", "TLSv1.2").filter { it in supported }
        check("TLSv1.2" in enabled) { "This Android device does not support TLS 1.2" }
        server.enabledProtocols = enabled.toTypedArray()
        return server
    }

    fun clearSessionKey() {
        val alias = activeAlias ?: return
        try {
            KeyStore.getInstance("AndroidKeyStore").apply {
                load(null)
                if (containsAlias(alias)) deleteEntry(alias)
            }
        } finally {
            activeAlias = null
            AgentRuntime.verificationCode = null
        }
    }

    fun verificationCode(certificateDer: ByteArray): String {
        val digest = java.security.MessageDigest.getInstance("SHA-256").digest(certificateDer)
        val code = digest.take(5).joinToString("") { "%02X".format(it.toInt() and 0xFF) }
        return "${code.substring(0, 5)}-${code.substring(5)}"
    }
}
