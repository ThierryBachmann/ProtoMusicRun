using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace MusicRun
{

    [System.Serializable]
    public class AuthResponse
    {
        public string kind;
        public string idToken;
        public string refreshToken;
        public string expiresIn;
        public string localId; // This is the user ID we'll use
    }

    [System.Serializable]
    public class AuthRequest
    {
        public bool returnSecureToken = true;
    }

    /*
     {
      "rules": {
        ".read": "now < 1754776800000",  // 2025-8-10
        ".write": "now < 1754776800000",  // 2025-8-10
        "leaderboard": {
          ".indexOn": "score"
        }
      }
    }
    // 23/07/2025
    {
      "rules": {
        "leaderboard": {
          ".read": true,  // Lecture publique
          ".write": "auth != null && (!root.child('lastSubmission/' + auth.uid).exists() || now - root.child('lastSubmission/' + auth.uid).val() > 60000)",
          ".indexOn": "score"
        },
        "lastSubmission": {
          "$uid": {
            ".write": "auth != null && auth.uid == $uid"
          }
        }
      }
    }
     */

    public class FirebaseAuth : MonoBehaviour
    {
        [Header("Player Info")]
        //public string playerDisplayName = "";
        public bool isAuthenticated = false;

        private string userId = "";
        private string idToken = "";

        public System.Action<bool> OnAuthenticationComplete;


        void Start()
        {

            // Try to load saved auth data
            LoadSavedAuth();

            // If no saved auth, authenticate anonymously
            if (!isAuthenticated)
            {
                StartCoroutine(AuthenticateAnonymously());
            }
            else
                OnAuthenticationComplete?.Invoke(true);

        }

        public IEnumerator AuthenticateAnonymously()
        {
            Debug.Log($"AuthenticateAnonymously");

            FirebaseKey key = new FirebaseKey();

            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={key.firebaseApiKey}";

            AuthRequest authRequest = new AuthRequest();
            string json = JsonUtility.ToJson(authRequest);

            UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);

                    userId = response.localId;
                    idToken = response.idToken;
                    isAuthenticated = true;

                    // Save auth data locally (survives browser sessions)
                    SaveAuthData();

                    // Generate or load player display name
                    //if (string.IsNullOrEmpty(playerDisplayName))
                    //{
                    //    playerDisplayName = GeneratePlayerName();
                    //    SavePlayerName();
                    //}

                    Debug.Log($"Anonymous authentication successful! User ID: {userId}");
                    OnAuthenticationComplete?.Invoke(true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse auth response: {e.Message}");
                    OnAuthenticationComplete?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"Authentication failed: {request.error}");
                OnAuthenticationComplete?.Invoke(false);
            }
        }

        private void SaveAuthData()
        {
            // Save to PlayerPrefs (persists across browser sessions in WebGL)
            PlayerPrefs.SetString("firebase_user_id", userId);
            PlayerPrefs.SetString("firebase_id_token", idToken);
            PlayerPrefs.SetString("auth_timestamp", DateTime.Now.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        private void LoadSavedAuth()
        {
            if (PlayerPrefs.HasKey("firebase_user_id"))
            {
                userId = PlayerPrefs.GetString("firebase_user_id");
                idToken = PlayerPrefs.GetString("firebase_id_token");

                // Check if token is still valid (tokens expire after 1 hour)
                if (PlayerPrefs.HasKey("auth_timestamp"))
                {
                    long timestamp = Convert.ToInt64(PlayerPrefs.GetString("auth_timestamp"));
                    DateTime authTime = DateTime.FromBinary(timestamp);

                    if (DateTime.Now - authTime < TimeSpan.FromMinutes(50)) // Refresh before expiry
                    {
                        isAuthenticated = true;
                        Debug.Log($"Loaded saved authentication");
                    }
                }
            }
            Debug.Log($"LoadSavedAuth User ID: {userId} {isAuthenticated}");
        }

     


        //public void SetPlayerName(string newName)
        //{
        //    if (!string.IsNullOrEmpty(newName) && newName.Length <= 20)
        //    {
        //        playerDisplayName = newName;
        //        SavePlayerName();
        //    }
        //}

        public string GetUserId()
        {
            return userId;
        }

        public string GetIdToken()
        {
            return idToken;
        }

        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        // Call this before making authenticated requests to Firebase
        public IEnumerator RefreshTokenIfNeeded()
        {
            if (!isAuthenticated)
                yield break;

            // Check if token needs refresh
            if (PlayerPrefs.HasKey("auth_timestamp"))
            {
                long timestamp = Convert.ToInt64(PlayerPrefs.GetString("auth_timestamp"));
                DateTime authTime = DateTime.FromBinary(timestamp);

                if (DateTime.Now - authTime > TimeSpan.FromMinutes(50))
                {
                    // Token is about to expire, refresh it
                    yield return RefreshToken();
                }
            }
        }

        private IEnumerator RefreshToken()
        {
            // Implement token refresh logic if needed
            // For anonymous auth, it's often easier to just re-authenticate
            Debug.Log("RefreshToken");
            yield return AuthenticateAnonymously();
        }
    }
}