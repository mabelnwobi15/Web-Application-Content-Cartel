package com.example.contentcartel.ui

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import com.example.contentcartel.R
import com.example.contentcartel.utils.BiometricHelper

class QuickLoginActivity : AppCompatActivity() {

    private lateinit var biometricHelper: BiometricHelper

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_quick_login)

        biometricHelper = BiometricHelper(this)

        val loginButton =
            findViewById<Button>(R.id.btnQuickLogin)

        val backButton =
            findViewById<TextView>(R.id.txtBackLogin)

        loginButton.setOnClickListener {

            showBiometricQuestion()
        }

        backButton.setOnClickListener {

            startActivity(
                Intent(this, LoginActivity::class.java)
            )

            finish()
        }
    }

    private fun showBiometricQuestion() {

        AlertDialog.Builder(this)
            .setTitle("Welcome to Content Cartel, Jordan")
            .setMessage(
                "Do you wish to use biometric Quick Login next time for convenience?"
            )
            .setNegativeButton("NO") { dialog, _ ->

                dialog.dismiss()

                loginNormally()
            }
            .setPositiveButton("YES") { dialog, _ ->

                dialog.dismiss()

                biometricHelper.authenticate(

                    onSuccess = {

                        Toast.makeText(
                            this,
                            "Biometric login successful",
                            Toast.LENGTH_SHORT
                        ).show()

                        goToHome()
                    },

                    onError = { message ->

                        Toast.makeText(
                            this,
                            message,
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                )
            }
            .show()
    }

    private fun loginNormally() {

        Toast.makeText(
            this,
            "Use your normal login credentials.",
            Toast.LENGTH_SHORT
        ).show()

        startActivity(
            Intent(this, LoginActivity::class.java)
        )

        finish()
    }

    private fun goToHome() {

        startActivity(
            Intent(this, HomeActivity::class.java)
        )

        finish()
    }
}