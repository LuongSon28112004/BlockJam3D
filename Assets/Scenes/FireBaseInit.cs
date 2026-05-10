using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    private FirebaseFirestore db;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                Debug.Log("Firebase Ready");

                db = FirebaseFirestore.DefaultInstance;

                GetUserData();
            }
            else
            {
                Debug.LogError("Firebase Error: " + status);
            }
        });
    }

    void GetUserData()
    {
        db.Collection("UserData")
          .Document("Son28112004")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  DocumentSnapshot snapshot = task.Result;

                  if (snapshot.Exists)
                  {
                      Dictionary<string, object> data = snapshot.ToDictionary();

                      Debug.Log("=== USER DATA ===");
                      Debug.Log("Coin: " + data["Coin"]);
                      Debug.Log("Level: " + data["Level"]);
                      Debug.Log("Heart: " + data["Heart"]);
                      Debug.Log("Frame: " + data["Frame"]);
                      Debug.Log("Name: " + data["Name"]);
                      Debug.Log("Id: " + data["Id"]);
                  }
                  else
                  {
                      Debug.Log("Document not found!");
                  }
              }
              else
              {
                  Debug.LogError("Get failed: " + task.Exception);
              }
          });
    }
}