using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System.Collections.Generic;

public class FirestoreTest : MonoBehaviour
{
    FirebaseFirestore db;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase Ready");

                db = FirebaseFirestore.DefaultInstance;

                TestRead();
                TestWrite();
            }
            else
            {
                Debug.LogError("Firebase lỗi: " + task.Result);
            }
        });
    }

    // ===== READ =====
    void TestRead()
    {
        db.Collection("UserData")
          .Document("ThanhVinh37")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  var snapshot = task.Result;

                  if (snapshot.Exists)
                  {
                      Debug.Log("=== READ SUCCESS ===");

                      var data = snapshot.ToDictionary();

                      foreach (var item in data)
                      {
                          Debug.Log(item.Key + " : " + item.Value);
                      }
                  }
                  else
                  {
                      Debug.Log("Không có document!");
                  }
              }
              else
              {
                  Debug.LogError("Read lỗi: " + task.Exception);
              }
          });
    }

    // ===== WRITE =====
    void TestWrite()
    {
        Dictionary<string, object> newData = new Dictionary<string, object>()
        {
            { "Coin", Random.Range(0,100) },
            { "Level", 999 },
            { "Heart", 10 },
            { "Frame", 1 },
            { "Name", "ThanhVinh37" }
        };

        db.Collection("UserData")
          .Document("ThanhVinh37")
          .UpdateAsync(newData)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  Debug.Log("=== WRITE SUCCESS ===");
              }
              else
              {
                  Debug.LogError("Write lỗi: " + task.Exception);
              }
          });
    }
}