package com.example.contentcartel.utils

import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity

class BiometricHelper(
    private val activity: FragmentActivity
) {

    fun authenticate(
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {

        val biometricManager =
            BiometricManager.from(activity)

        val canAuthenticate =
            biometricManager.canAuthenticate(
                BiometricManager.Authenticators.BIOMETRIC_STRONG
            )

        if (canAuthenticate !=
            BiometricManager.BIOMETRIC_SUCCESS
        ) {

            onError(
                "Biometric authentication is not available."
            )

            return
        }

        val executor =
            ContextCompat.getMainExecutor(activity)

        val biometricPrompt =
            BiometricPrompt(
                activity,
                executor,
                object :
                    BiometricPrompt.AuthenticationCallback() {

                    override fun onAuthenticationSucceeded(
                        result: BiometricPrompt.AuthenticationResult
                    ) {
                        super.onAuthenticationSucceeded(result)

                        onSuccess()
                    }

                    override fun onAuthenticationError(
                        errorCode: Int,
                        errString: CharSequence
                    ) {
                        super.onAuthenticationError(
                            errorCode,
                            errString
                        )

                        onError(errString.toString())
                    }
                }
            )

        val promptInfo =
            BiometricPrompt.PromptInfo.Builder()
                .setTitle("Content Cartel")
                .setSubtitle("Quick Login")
                .setDescription(
                    "Use your fingerprint or face to login."
                )
                .setNegativeButtonText("CANCEL")
                .build()

        biometricPrompt.authenticate(promptInfo)
    }
}