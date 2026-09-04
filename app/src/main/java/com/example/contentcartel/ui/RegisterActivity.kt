package com.example.contentcartel.ui

import android.content.Intent
import android.os.Bundle
import android.util.Patterns
import android.widget.Button
import android.widget.CheckBox
import android.widget.EditText
import android.widget.ImageButton
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.contentcartel.R
import com.example.contentcartel.data.AuthRepository
import com.example.contentcartel.data.UserProfile

class RegisterActivity : AppCompatActivity() {

    private lateinit var authRepository: AuthRepository

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_register)

        authRepository = AuthRepository()

        val name = findViewById<EditText>(R.id.edtName)
        val surname = findViewById<EditText>(R.id.edtSurname)
        val username = findViewById<EditText>(R.id.edtUsername)
        val email = findViewById<EditText>(R.id.edtEmail)
        val phone = findViewById<EditText>(R.id.edtPhone)
        val password = findViewById<EditText>(R.id.edtPassword)
        val confirmPassword =
            findViewById<EditText>(R.id.edtConfirmPassword)

        val robotCheck =
            findViewById<CheckBox>(R.id.checkRobot)

        val btnBack = findViewById<ImageButton>(R.id.btnBack)
        btnBack.setOnClickListener {
            finish()
        }

        val registerButton =
            findViewById<Button>(R.id.btnRegister)

        registerButton.setOnClickListener {
            val nameValue = name.text.toString().trim()
            val surnameValue = surname.text.toString().trim()
            val usernameValue = username.text.toString().trim()
            val emailValue = email.text.toString().trim()
            val phoneValue = phone.text.toString().trim()
            val passwordValue = password.text.toString()
            val confirmPasswordValue =
                confirmPassword.text.toString()

            if (nameValue.isEmpty()) {
                name.error = "Enter your name"
                return@setOnClickListener
            }

            if (surnameValue.isEmpty()) {
                surname.error = "Enter your surname"
                return@setOnClickListener
            }

            if (usernameValue.isEmpty()) {
                username.error = "Enter a username"
                return@setOnClickListener
            }

            if (!Patterns.EMAIL_ADDRESS
                    .matcher(emailValue)
                    .matches()
            ) {
                email.error = "Enter a valid email"
                return@setOnClickListener
            }

            if (phoneValue.isEmpty()) {
                phone.error = "Enter your phone number"
                return@setOnClickListener
            }

            if (passwordValue.length < 6) {
                password.error =
                    "Password must be at least 6 characters"
                return@setOnClickListener
            }

            if (passwordValue != confirmPasswordValue) {
                confirmPassword.error =
                    "Passwords do not match"
                return@setOnClickListener
            }

            if (!robotCheck.isChecked) {
                Toast.makeText(
                    this,
                    "Please confirm that you are not a robot.",
                    Toast.LENGTH_SHORT
                ).show()

                return@setOnClickListener
            }

            registerButton.isEnabled = false

            val profile = UserProfile(
                name = nameValue,
                surname = surnameValue,
                username = usernameValue,
                email = emailValue,
                phone = phoneValue
            )

            authRepository.registerUser(
                profile = profile,
                password = passwordValue,

                onSuccess = {

                    Toast.makeText(
                        this,
                        "Account created successfully",
                        Toast.LENGTH_SHORT
                    ).show()

                    startActivity(
                        Intent(
                            this,
                            LoginActivity::class.java
                        )
                    )

                    finish()
                },

                onError = { message ->

                    registerButton.isEnabled = true

                    Toast.makeText(
                        this,
                        message,
                        Toast.LENGTH_LONG
                    ).show()
                }
            )
        }
    }
}