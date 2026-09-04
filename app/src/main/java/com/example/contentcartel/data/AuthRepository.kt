package com.example.contentcartel.data


import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.database.FirebaseDatabase

class AuthRepository {

    private val auth = FirebaseAuth.getInstance()

    // Firebase Realtime Database
    private val database = FirebaseDatabase.getInstance(
        "https://contentcartel-62294-default-rtdb.firebaseio.com/"
    )

    private val usersRef = database.getReference("users")

    fun registerUser(
        profile: UserProfile,
        password: String,
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {

        auth.createUserWithEmailAndPassword(
            profile.email,
            password
        )
            .addOnSuccessListener { result ->

                val uid = result.user?.uid

                if (uid == null) {
                    onError("Could not create user.")
                    return@addOnSuccessListener
                }

                val newProfile = profile.copy(uid = uid)

                usersRef.child(uid)
                    .setValue(newProfile)
                    .addOnSuccessListener {
                        onSuccess()
                    }
                    .addOnFailureListener { exception ->
                        onError(
                            exception.message
                                ?: "Could not save user profile."
                        )
                    }
            }
            .addOnFailureListener { exception ->
                onError(
                    exception.message
                        ?: "Registration failed."
                )
            }
    }

    fun loginWithEmail(
        email: String,
        password: String,
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {

        auth.signInWithEmailAndPassword(
            email,
            password
        )
            .addOnSuccessListener {
                onSuccess()
            }
            .addOnFailureListener { exception ->
                onError(
                    exception.message
                        ?: "Login failed."
                )
            }
    }

    fun getEmailFromUsername(
        username: String,
        onSuccess: (String) -> Unit,
        onError: (String) -> Unit
    ) {

        usersRef
            .orderByChild("username")
            .equalTo(username)
            .limitToFirst(1)
            .get()
            .addOnSuccessListener { snapshot ->

                if (!snapshot.exists()) {
                    onError("Username not found.")
                    return@addOnSuccessListener
                }

                val userSnapshot = snapshot.children.firstOrNull()

                val email = userSnapshot
                    ?.child("email")
                    ?.getValue(String::class.java)

                if (email == null) {
                    onError("User email not found.")
                } else {
                    onSuccess(email)
                }
            }
            .addOnFailureListener { exception ->
                onError(
                    exception.message
                        ?: "Could not find username."
                )
            }
    }

    fun loginWithUsername(
        username: String,
        password: String,
        onSuccess: () -> Unit,
        onError: (String) -> Unit
    ) {

        getEmailFromUsername(
            username = username,
            onSuccess = { email ->

                loginWithEmail(
                    email = email,
                    password = password,
                    onSuccess = onSuccess,
                    onError = onError
                )
            },
            onError = onError
        )
    }

    fun getCurrentUser() = auth.currentUser

    fun logout() {
        auth.signOut()
    }
}

