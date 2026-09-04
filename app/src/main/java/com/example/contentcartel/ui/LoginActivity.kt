package com.example.contentcartel.ui

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.contentcartel.R
import com.example.contentcartel.data.AuthRepository

class LoginActivity : AppCompatActivity() {

    private lateinit var authRepository: AuthRepository

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_login)

        authRepository = AuthRepository()

        val username = findViewById<EditText>(R.id.edtUsername)
        val password = findViewById<EditText>(R.id.edtPassword)

        val loginButton = findViewById<Button>(R.id.btnLogin)
        val registerText = findViewById<TextView>(R.id.txtRegister)

        loginButton.setOnClickListener {

            val usernameValue = username.text.toString().trim()
            val passwordValue = password.text.toString()

            if (usernameValue.isEmpty()) {
                username.error = "Enter your username"
                return@setOnClickListener
            }

            if (passwordValue.isEmpty()) {
                password.error = "Enter your password"
                return@setOnClickListener
            }

            loginButton.isEnabled = false

            authRepository.loginWithUsername(
                username = usernameValue,
                password = passwordValue,

                onSuccess = {

                    Toast.makeText(
                        this,
                        "Login successful",
                        Toast.LENGTH_SHORT
                    ).show()

                    startActivity(
                        Intent(this, HomeActivity::class.java)
                    )

                    finish()
                },

                onError = { message ->

                    loginButton.isEnabled = true

                    Toast.makeText(
                        this,
                        message,
                        Toast.LENGTH_LONG
                    ).show()
                }
            )
        }

        registerText.setOnClickListener {

            startActivity(
                Intent(this, RegisterActivity::class.java)
            )
        }
    }
}