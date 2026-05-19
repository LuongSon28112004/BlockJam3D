using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using master;
using Firebase.Database;
using Firebase.Database;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using System.Linq;

public class UserDataFirebaseManager : SingletonDDOL<UserDataFirebaseManager>
{
    private FirebaseFirestore db;
    private const string COLLECTION_NAME = "UserData";

    public string CurrentUserId { get; private set; }
    public string CurrentUserName { get; private set; }
    public bool IsFirebaseInitialized { get; private set; }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                IsFirebaseInitialized = true;
                // Khởi tạo db khi script bắt đầu
                db = FirebaseFirestore.DefaultInstance;

                CheckAndInitializeUser();
            }
            else
            {
                Debug.LogError(string.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }


    /// <summary>
    /// Lắng nghe các lời mời kết bạn mới thông qua Realtime Database. Khi có lời mời mới, sẽ nhận được callback trong OnFriendRequestAdded để xử lý (ví dụ: hiển thị popup thông báo, load info người gửi từ Firestore, hiện nút Accept/Reject, v.v...). Lưu ý: đây chỉ là phần lắng nghe realtime để cập nhật UI ngay khi có lời mời mới. Logic lưu trữ và quản lý lời mời vẫn nên được thực hiện trong Firestore để đảm bảo tính nhất quán và dễ dàng truy vấn.
    /// </summary>
    private DatabaseReference friendRequestRef;
    private DatabaseReference friendAcceptRef;
    private DatabaseReference friendDeclineRef;
    private DatabaseReference boosterRef;

    public void StartListeningFriendRequest(string myUserId)
    {
        var db = FirebaseDatabase.GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app");

        // Friend Request
        friendRequestRef = db.GetReference("friend_requests").Child(myUserId);
        friendRequestRef.ChildAdded += OnFriendRequestAdded;

        // Friend Accept
        friendAcceptRef = db.GetReference("friend_accept").Child(myUserId);
        friendAcceptRef.ChildAdded += OnFriendAcceptAdded;

        // Friend Decline
        friendDeclineRef = db.GetReference("friend_decline").Child(myUserId);
        friendDeclineRef.ChildAdded += OnFriendDeclineAdded;

        // Booster receive
        boosterRef = db.GetReference("send_booster").Child(myUserId);
        boosterRef.ChildAdded += OnBoosterReceived;

        Debug.Log("[Realtime] Listening all friend events...");
    }

    public void StopListeningFriendRequest()
    {
        if (friendRequestRef != null)
        {
            friendRequestRef.ChildAdded -= OnFriendRequestAdded;
            friendRequestRef = null;
        }

        if (friendAcceptRef != null)
        {
            friendAcceptRef.ChildAdded -= OnFriendAcceptAdded;
            friendAcceptRef = null;
        }

        if (friendDeclineRef != null)
        {
            friendDeclineRef.ChildAdded -= OnFriendDeclineAdded;
            friendDeclineRef = null;
        }

        if (boosterRef != null)
        {
            boosterRef.ChildAdded -= OnBoosterReceived;
            boosterRef = null;
        }

        Debug.Log("[Realtime] Stopped listening all friend events.");
    }



    private void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string fromUserId = args.Snapshot.Key;

            Debug.Log($"[Realtime] New friend request from: {fromUserId}");

            // TODO:
            // - Hiện popup UI
            // - Load info user từ Firestore
            // - Hiện nút Accept / Reject
            UIManager.Instance.NotifyContent(Loc.Get("friend_new_request_fmt", fromUserId));
            // XÓA NGAY để tránh trigger lại
            friendRequestRef.Child(fromUserId).RemoveValueAsync();
        }
    }

    private void OnFriendAcceptAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string toUserId = args.Snapshot.Key;

            Debug.Log($"[Realtime] Friend accepted: {toUserId}");

            UIManager.Instance.NotifyContent(Loc.Get("friend_request_accepted_by_fmt", toUserId));

            // XÓA để tránh bị trigger lại
            friendAcceptRef.Child(toUserId).RemoveValueAsync();
            LeaderBoardManager.onUpdateFriendList?.Invoke();
        }
    }
    private void OnFriendDeclineAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            string toUserId = args.Snapshot.Key;

            Debug.Log($"[Realtime] Friend declined: {toUserId}");

            UIManager.Instance.NotifyContent(Loc.Get("friend_request_declined_by_fmt", toUserId));

            // XÓA để tránh bị trigger lại
            friendDeclineRef.Child(toUserId).RemoveValueAsync();
        }
    }

    private void OnBoosterReceived(object sender, ChildChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            var data = args.Snapshot.Value as Dictionary<string, object>;

            string fromUserId = data["fromUserId"].ToString();
            string boosterName = data["boosterName"].ToString();
            int amount = Convert.ToInt32(data["amount"]);

            // =========================
            // 🎯 UPDATE LOCAL DATA
            // =========================

            if (boosterName == "Heart")
            {
                HeartManager.Instance.Add(amount);

                UIManager.Instance.NotifyContent(Loc.Get("received_hearts_fmt", amount, fromUserId));
            }
            else
            {
                // Update booster list
                if (UserData.listBoosterCounters == null)
                    UserData.listBoosterCounters = new List<BoosterCounter>();

                bool found = false;

                foreach (var booster in UserData.listBoosterCounters)
                {
                    if (booster.name == boosterName)
                    {
                        booster.count += amount;
                        found = true;
                        break;
                    }
                }

                // nếu chưa có booster đó
                if (!found)
                {
                    UserData.listBoosterCounters.Add(new BoosterCounter
                    {
                        name = boosterName,
                        count = amount
                    });
                }

                UIManager.Instance.NotifyContent(Loc.Get("received_booster_fmt", boosterName, amount, fromUserId));
            }

            // =========================
            // 💾 SAVE LOCAL + FIREBASE
            // =========================
            SaveDataManager.Save();

            // =========================
            // 🧹 REMOVE EVENT
            // =========================
            boosterRef.Child(args.Snapshot.Key).RemoveValueAsync();
        }
    }

    public void AddTestUsersSequential()
    {
        CreateUserRecursive(0);
    }

    private void CreateUserRecursive(int count)
    {
        if (count >= 10) return;

        GenerateUniqueId(newId =>
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
            { "Id", newId },
            { "Name", "Bot_" + newId },
            { "Coin", UnityEngine.Random.Range(0, 10000) },
            { "Level", UnityEngine.Random.Range(1, 50) },
            { "Heart", UnityEngine.Random.Range(1, 5) },
            { "Frame", 0 },

            // 👇 THÊM BOOSTERS
            { "Boosters", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "name", "Freeze" },
                        { "count", UnityEngine.Random.Range(0, 5) }
                    },
                    new Dictionary<string, object>
                    {
                        { "name", "Bomb" },
                        { "count", UnityEngine.Random.Range(0, 5) }
                    },
                    new Dictionary<string, object>
                    {
                        { "name", "Hammer" },
                        { "count", UnityEngine.Random.Range(0, 5) }
                    }
                }
            },

            { "CreatedAt", FieldValue.ServerTimestamp }
            };

            SaveUserData(newId, data, success =>
            {
                if (success)
                {
                    Debug.Log($"Created test user: {newId}");
                    CreateUserRecursive(count + 1);
                }
            });
        });
    }

    /// <summary>
    /// Kiểm tra người chơi ở local, nếu chưa có thì tạo mới ID (bắt đầu từ 10000000 không trùng lặp) và lưu vào Firebase.
    /// </summary>
    public void CheckAndInitializeUser()
    {
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            CurrentUserId = PlayerPrefs.GetString("PlayerID");
            CurrentUserName = PlayerPrefs.GetString("PlayerName");
            Debug.Log($"[LocalUser] Welcome back, {CurrentUserName} (ID: {CurrentUserId})");
            StartListeningFriendRequest(CurrentUserId);
        }
        else
        {
            Debug.Log("[LocalUser] No local data found. Generating new user ID...");
            GenerateUniqueId(newId =>
            {
                CurrentUserId = newId;
                CurrentUserName = "Player" + newId;

                PlayerPrefs.SetString("PlayerID", CurrentUserId);
                PlayerPrefs.SetString("PlayerName", CurrentUserName);
                PlayerPrefs.Save();

                Debug.Log($"[LocalUser] Created new user locally: {CurrentUserName} (ID: {CurrentUserId})");

                // Thiết lập booster mặc định cho người chơi mới
                UserData.listBoosterCounters = new List<BoosterCounter>
                {
                  new BoosterCounter { name = "Undo", count = 2 },
                new BoosterCounter { name = "Add", count = 2 },
                new BoosterCounter { name = "Shuffle", count = 2 },
                new BoosterCounter { name = "Magnet", count = 2 }
                };

                // Lưu dữ liệu ban đầu của người chơi lên Firebase
                Dictionary<string, object> initialData = new Dictionary<string, object>
                {
                    { "Id", CurrentUserId },
                    { "Name", CurrentUserName },
                    { "Coin", 30 },
                    { "Level", 1 },
                    { "Heart", 5 },
                    { "Frame", 0 },
                    { "Boosters", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "name", "Undo" }, { "count", 1 } },
                            new Dictionary<string, object> { { "name", "Add" }, { "count", 1 } },
                            new Dictionary<string, object> { { "name", "Shuffle" }, { "count", 1 } },
                            new Dictionary<string, object> { { "name", "Magnet" }, { "count", 1 } }
                        }
                    },
                    { "CreatedAt", FieldValue.ServerTimestamp }
                };
                SaveUserData(CurrentUserId, initialData);
                // lưu data heart
                PlayerPrefs.SetInt("Hearts", 5);
                PlayerPrefs.SetFloat("Timer", 0);
                PlayerPrefs.SetString("LastQuitTime", DateTime.Now.ToBinary().ToString());
                PlayerPrefs.Save();
                StartListeningFriendRequest(CurrentUserId);
            });
        }
    }

    private void GenerateUniqueId(Action<string> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        DocumentReference counterRef = db.Collection("ServerConfigs").Document("UserCounter");

        db.RunTransactionAsync(transaction =>
        {
            return transaction.GetSnapshotAsync(counterRef).ContinueWith(task =>
            {
                DocumentSnapshot snapshot = task.Result;
                long nextId = 10000000;

                if (snapshot.Exists && snapshot.TryGetValue("LastUserID", out long lastId))
                {
                    if (lastId >= 10000000)
                    {
                        nextId = lastId + 1;
                    }
                }

                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "LastUserID", nextId }
                };

                transaction.Set(counterRef, updates, SetOptions.MergeAll);
                return nextId;
            });
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                onComplete?.Invoke(task.Result.ToString());
            }
            else
            {
                Debug.LogWarning($"[Firebase] Failed to generate sequential ID: {task.Exception}. Using fallback timestamp ID.");
                // Fallback nếu offline hoặc lỗi mạng: dùng timestamp để có ID duy nhất (>= 10000000)
                long fallbackId = 10000000 + (long)(DateTime.UtcNow - new DateTime(2024, 1, 1)).TotalSeconds;
                onComplete?.Invoke(fallbackId.ToString());
            }
        });
    }

    /// <summary>
    /// Lưu hoặc cập nhật data cho một người dùng (documentId thường là Id người dùng)
    /// </summary>
    public void SaveUserData(string documentId, Dictionary<string, object> data, Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
            Debug.LogError("[Firebase] Firestore is not initialized.");
            onComplete?.Invoke(false);
            return;
        }

        DocumentReference docRef = db.Collection(COLLECTION_NAME).Document(documentId);

        // Dùng SetOptions.MergeAll để cập nhật các trường được truyền vào, giữ nguyên các trường khác
        docRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"[Firebase] UserData {documentId} saved successfully!");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[Firebase] Failed to save UserData {documentId}: {task.Exception}");
                onComplete?.Invoke(false);
            }
        });
    }

    public void UpdateHeart(int newHearts)
    {
        if (string.IsNullOrEmpty(CurrentUserId)) return;
        SaveUserData(CurrentUserId, new Dictionary<string, object> { { "Heart", newHearts } });
    }

    /// <summary>
    /// Đăng nhập bằng Unity Player Account. Yêu cầu cài 2 package:
    ///   - com.unity.services.core
    ///   - com.unity.services.authentication
    /// và define symbol UNITY_PLAYER_ID trong Player Settings (Scripting Define Symbols).
    /// Sau khi đăng nhập thành công, AuthenticationService.PlayerId sẽ thay thế CurrentUserId hiện tại
    /// và document tương ứng trong Firestore sẽ được tạo/merge.
    /// </summary>
    public void LoginWithUnityPlayer(Action<bool, string> onComplete = null)
    {
        if (!IsFirebaseInitialized)
        {
            Debug.LogWarning("[UnityLogin] Firebase chưa init xong, thử lại sau.");
            if (UIManager.Instance != null) UIManager.Instance.NotifyContent(Loc.Get("firebase_not_ready"));
            onComplete?.Invoke(false, "firebase_not_ready");
            return;
        }

#if UNITY_PLAYER_ID
        _ = LoginWithUnityPlayerAsync(onComplete);
#else
        Debug.LogWarning("[UnityLogin] Package com.unity.services.authentication chưa được cài hoặc symbol UNITY_PLAYER_ID chưa được define.");
        if (UIManager.Instance != null) UIManager.Instance.NotifyContent(Loc.Get("unity_pid_not_configured"));
        onComplete?.Invoke(false, "package_missing");
#endif
    }

#if UNITY_PLAYER_ID
    private async Task LoginWithUnityPlayerAsync(Action<bool, string> onComplete)
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                OnUnityLoginSuccess(onComplete);
                return;
            }

            TaskCompletionSource<bool> signInTcs = new TaskCompletionSource<bool>();

            Action onSignedIn = null;
            Action<string> onSignInFailed = null;

            onSignedIn = () =>
            {
                PlayerAccountService.Instance.SignedIn -= onSignedIn;
                PlayerAccountService.Instance.SignInFailed -= onSignInFailed;
                signInTcs.TrySetResult(true);
            };
            onSignInFailed = (err) =>
            {
                PlayerAccountService.Instance.SignedIn -= onSignedIn;
                PlayerAccountService.Instance.SignInFailed -= onSignInFailed;
                signInTcs.TrySetException(new Exception(err));
            };

            PlayerAccountService.Instance.SignedIn += onSignedIn;
            PlayerAccountService.Instance.SignInFailed += onSignInFailed;

            await PlayerAccountService.Instance.StartSignInAsync();
            await signInTcs.Task;

            string accessToken = PlayerAccountService.Instance.AccessToken;
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);

            OnUnityLoginSuccess(onComplete);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnityLogin] Failed: {ex}");
            if (UIManager.Instance != null) UIManager.Instance.NotifyContent(Loc.Get("unity_pid_login_failed"));
            onComplete?.Invoke(false, ex.Message);
        }
    }

    private void OnUnityLoginSuccess(Action<bool, string> onComplete)
    {
        string previousUserId = CurrentUserId;
        string playerId = AuthenticationService.Instance.PlayerId;
        string playerName = AuthenticationService.Instance.PlayerInfo?.Username;

        CurrentUserId = playerId;
        if (!string.IsNullOrEmpty(playerName)) CurrentUserName = playerName;

        PlayerPrefs.SetString("PlayerID", CurrentUserId);
        PlayerPrefs.SetString("PlayerName", CurrentUserName);
        PlayerPrefs.Save();

        Debug.Log($"[UnityLogin] Signed in as {CurrentUserName} ({CurrentUserId}). Previous local id: {previousUserId}");

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "Id", CurrentUserId },
            { "Name", CurrentUserName },
            { "Provider", "unity" },
            { "LastLoginAt", FieldValue.ServerTimestamp }
        };
        SaveUserData(CurrentUserId, data);

        StartListeningFriendRequest(CurrentUserId);

        if (UIManager.Instance != null) UIManager.Instance.NotifyContent(Loc.Get("welcome_back_fmt", CurrentUserName));
        onComplete?.Invoke(true, CurrentUserId);
    }
#endif

    public void SignOutUnityPlayer()
    {
#if UNITY_PLAYER_ID
        if (AuthenticationService.Instance.IsSignedIn) AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        Debug.Log("[UnityLogin] Signed out.");
#endif
    }

    /// <summary>
    /// Lấy data của một người dùng
    /// </summary>
    public void GetUserData(string documentId, Action<Dictionary<string, object>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
            Debug.LogError("[Firebase] Firestore is not initialized.");
            onComplete?.Invoke(null);
            return;
        }

        DocumentReference docRef = db.Collection(COLLECTION_NAME).Document(documentId);
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    Debug.Log($"[Firebase] UserData {documentId} retrieved successfully!");
                    onComplete?.Invoke(data);
                }
                else
                {
                    Debug.LogWarning($"[Firebase] Document {documentId} does not exist!");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[Firebase] Failed to get UserData {documentId}: {task.Exception}");
                onComplete?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Xoá data của một người dùng
    /// </summary>
    public void DeleteUserData(string documentId, Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
            Debug.LogError("[Firebase] Firestore is not initialized.");
            onComplete?.Invoke(false);
            return;
        }

        DocumentReference docRef = db.Collection(COLLECTION_NAME).Document(documentId);
        docRef.DeleteAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"[Firebase] UserData {documentId} deleted successfully!");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[Firebase] Failed to delete UserData {documentId}: {task.Exception}");
                onComplete?.Invoke(false);
            }
        });
    }

    public void GetAllUsers(Action<List<Dictionary<string, object>>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        db.Collection(COLLECTION_NAME).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                QuerySnapshot snapshot = task.Result;

                List<Dictionary<string, object>> users = new List<Dictionary<string, object>>();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        users.Add(doc.ToDictionary());
                    }
                }

                Debug.Log($"[Firebase] Loaded {users.Count} users");
                onComplete?.Invoke(users);
            }
            else
            {
                Debug.LogError($"[Firebase] Failed to get users: {task.Exception}");
                onComplete?.Invoke(null);
            }
        });
    }

    /// <summary>
    /// Thêm bạn bè giữa 2 người dùng (tạo document trong subcollection "Friends" của mỗi người, chứa Id của người kia)
    /// </summary>
    /// <param name="userAId"></param>
    /// <param name="userBId"></param>
    /// <param name="onComplete"></param>
    public void AddFriend(string userAId, string userBId, Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        WriteBatch batch = db.StartBatch();

        DocumentReference userARef = db.Collection(COLLECTION_NAME)
                                       .Document(userAId)
                                       .Collection("Friends")
                                       .Document(userBId);

        DocumentReference userBRef = db.Collection(COLLECTION_NAME)
                                       .Document(userBId)
                                       .Collection("Friends")
                                       .Document(userAId);

        Dictionary<string, object> dataA = new Dictionary<string, object>
    {
        { "Id", userBId },
        { "CreatedAt", FieldValue.ServerTimestamp }
    };

        Dictionary<string, object> dataB = new Dictionary<string, object>
    {
        { "Id", userAId },
        { "CreatedAt", FieldValue.ServerTimestamp }
    };

        batch.Set(userARef, dataA);
        batch.Set(userBRef, dataB);

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"[Friend] {userAId} and {userBId} are now friends!");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[Friend] AddFriend failed: {task.Exception}");
                onComplete?.Invoke(false);
            }
        });
    }
    /// <summary>
    /// Gửi lời mời kết bạn từ fromUserId đến toUserId. Lưu ý: để đơn giản, ở đây chúng ta chỉ lưu một document trong Firestore và một node trong Realtime Database để thông báo cho người nhận. Trong thực tế, bạn có thể muốn lưu thêm thông tin như trạng thái (đang chờ, đã chấp nhận, đã từ chối), thời gian gửi, v.v... và xử lý logic phức tạp hơn.
    /// </summary>
    /// <param name="fromUserId"></param>
    /// <param name="toUserId"></param>
    public void SendFriendRequest(string fromUserId, string toUserId, Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        var requestData = new Dictionary<string, object>
    {
        { "FromUserId", fromUserId },
        { "ToUserId", toUserId },
        {"FromUserName", CurrentUserName},
        { "Status", "Pending" },
        { "CreatedAt", FieldValue.ServerTimestamp }
    };

        var firestoreTask = db.Collection("FriendRequests").AddAsync(requestData);

        var realtimeRef = FirebaseDatabase
    .GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app")
    .RootReference;
        var realtimeTask = realtimeRef.Child("friend_requests")
                                      .Child(toUserId)
                                      .Child(fromUserId)
                                      .SetValueAsync(true);

        // Đợi cả 2 hoàn thành
        Task.WhenAll(firestoreTask, realtimeTask)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    Debug.Log("[FriendRequest] Sent successfully");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[FriendRequest] Failed: {task.Exception}");
                    onComplete?.Invoke(false);
                }
            });
    }

    /// <summary>
    /// Chấp nhận lời mời kết bạn từ fromUserId đến toUserId. Logic đơn giản là gọi AddFriend để tạo mối quan hệ bạn bè, sau đó gửi thông báo realtime để cập nhật UI cho người gửi. Lưu ý: trong thực tế, bạn nên cập nhật trạng thái của lời mời trong Firestore (ví dụ: từ "Pending" sang "Accepted") và xử lý các trường hợp như từ chối, hủy lời mời, v.v...
    /// </summary>
    /// <param name="fromUserId"></param>
    /// <param name="toUserId"></param>
    public void AcceptFriend(string fromUserId, string toUserId)
    {
        AddFriend(fromUserId, toUserId);

        // notify lại
        var realtimeRef = FirebaseDatabase
    .GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app")
    .RootReference;
        realtimeRef.Child("friend_accept")
                   .Child(fromUserId)
                   .Child(toUserId)
                   .SetValueAsync(true);
    }
    /// <summary>
    /// Lấy danh sách bạn bè của một người dùng (truy vấn subcollection "Friends", sau đó lấy data user thật của từng friendId)
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="onComplete"></param>
    public void GetFriendsList(string userId, Action<List<Dictionary<string, object>>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        CollectionReference friendsRef = db.Collection(COLLECTION_NAME)
                                           .Document(userId)
                                           .Collection("Friends");

        friendsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Friend] Failed to get friends list");
                onComplete?.Invoke(null);
                return;
            }

            QuerySnapshot snapshot = task.Result;

            List<Dictionary<string, object>> friendsData = new List<Dictionary<string, object>>();

            if (snapshot.Count == 0)
            {
                onComplete?.Invoke(friendsData);
                return;
            }

            int remaining = snapshot.Count;

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                string friendId = doc.Id;

                // Lấy data user thật
                GetUserData(friendId, userData =>
                {
                    if (userData != null)
                    {
                        friendsData.Add(userData);
                    }

                    remaining--;

                    if (remaining == 0)
                    {
                        Debug.Log($"[Friend] Loaded {friendsData.Count} friends");
                        onComplete?.Invoke(friendsData);
                    }
                });
            }
        });
    }

    /// <summary>
    /// Tìm người dùng theo Id chính xác hoặc prefix (để add friend). Lưu ý: tìm theo prefix có thể trả về nhiều kết quả, nên UI cần hiển thị danh sách để người chơi chọn đúng người muốn kết bạn.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="onComplete"></param>

    public void SearchUserByExactId(string userId, Action<Dictionary<string, object>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        db.Collection(COLLECTION_NAME)
          .Document(userId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  DocumentSnapshot snapshot = task.Result;
                  if (snapshot.Exists)
                  {
                      onComplete?.Invoke(snapshot.ToDictionary());
                  }
                  else
                  {
                      onComplete?.Invoke(null);
                  }
              }
              else
              {
                  Debug.LogError($"Search failed: {task.Exception}");
                  onComplete?.Invoke(null);
              }
          });
    }


    /// <summary>
    /// Tìm người dùng theo prefix của Id (ví dụ: nhập "1000001" sẽ trả về tất cả người dùng có Id bắt đầu bằng "1000001" như "10000010", "10000011",...). Lưu ý: tìm theo prefix có thể trả về nhiều kết quả, nên UI cần hiển thị danh sách để người chơi chọn đúng người muốn kết bạn.
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="onComplete"></param>
    public void SearchUsersByIdPrefix(string keyword, Action<List<Dictionary<string, object>>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        string end = keyword + "\uf8ff"; // trick để query prefix

        db.Collection(COLLECTION_NAME)
          .WhereGreaterThanOrEqualTo("Id", keyword)
          .WhereLessThanOrEqualTo("Id", end)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

                  foreach (var doc in task.Result.Documents)
                  {
                      result.Add(doc.ToDictionary());
                  }

                  Debug.Log($"[Search] Found {result.Count} users");
                  onComplete?.Invoke(result);
              }
              else
              {
                  Debug.LogError($"Search failed: {task.Exception}");
                  onComplete?.Invoke(null);
              }
          });
    }

    /// <summary>
    /// Lấy danh sách lời mời kết bạn đến cho người dùng (truy vấn collection "FriendRequests" với điều kiện ToUserId = myUserId và Status = "Pending"). Lưu ý: bạn có thể muốn mở rộng logic này để phân biệt giữa các trạng thái khác nhau của lời mời (ví dụ: đã chấp nhận, đã từ chối) và xử lý tương ứng trong UI.
    /// </summary>
    /// <param name="myUserId"></param>
    /// <param name="onComplete"></param>

    public void GetMyFriendRequests(string myUserId, Action<List<Dictionary<string, object>>> onComplete)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        db.Collection("FriendRequests")
          .WhereEqualTo("ToUserId", myUserId)
          .WhereEqualTo("Status", "Pending") // chỉ lấy lời mời đang chờ (tuỳ bạn)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  List<Dictionary<string, object>> requests = new List<Dictionary<string, object>>();

                  foreach (var doc in task.Result.Documents)
                  {
                      if (doc.Exists)
                      {
                          requests.Add(doc.ToDictionary());
                      }
                  }

                  Debug.Log($"[FriendRequest] Loaded {requests.Count} requests");
                  onComplete?.Invoke(requests);
              }
              else
              {
                  Debug.LogError($"[FriendRequest] Load failed: {task.Exception}");
                  onComplete?.Invoke(null);
              }
          });
    }


    /// <summary>
    /// Từ chối lời mời kết bạn từ fromUserId đến toUserId. Logic đơn giản là xoá document lời mời trong Firestore và gửi thông báo realtime để cập nhật UI cho người gửi. Lưu ý: trong thực tế, bạn nên cập nhật trạng
    /// trạng của lời mời trong Firestore (ví dụ: từ "Pending" sang "Declined") thay vì xoá hẳn, để có thể lưu lại lịch sử và xử lý các trường hợp phức tạp hơn.
    /// </summary> <param name="fromUserId"></param>
    /// <param name="toUserId"></param>
    /// <param name="onComplete"></param>
    public void DeclineFriendRequest(string fromUserId, string toUserId, Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null) db = FirebaseFirestore.DefaultInstance;

        // Xoá document lời mời trong Firestore
        db.Collection("FriendRequests")
          .WhereEqualTo("FromUserId", fromUserId)
          .WhereEqualTo("ToUserId", toUserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  foreach (var doc in task.Result.Documents)
                  {
                      doc.Reference.DeleteAsync();
                  }

                  Debug.Log("[FriendRequest] Declined successfully");
                  onComplete?.Invoke(true);
              }
              else
              {
                  Debug.LogError($"[FriendRequest] Decline failed: {task.Exception}");
                  onComplete?.Invoke(false);
              }
          });

        // notify lại
        var realtimeRef = FirebaseDatabase
    .GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app")
    .RootReference;
        realtimeRef.Child("friend_decline")
                   .Child(fromUserId)
                   .Child(toUserId)
                   .SetValueAsync(true);
    }


    ///accept friend request
    public void AcceptFriendRequest(string fromUserId, string toUserId, Action<bool> onComplete = null)
    {
        AddFriend(fromUserId, toUserId, success =>
        {
            if (success)
            {
                // Sau khi đã thêm bạn thành công, xoá lời mời trong Firestore
                db.Collection("FriendRequests")
                  .WhereEqualTo("FromUserId", fromUserId)
                  .WhereEqualTo("ToUserId", toUserId)
                  .GetSnapshotAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (task.IsCompleted && !task.IsFaulted)
                      {
                          foreach (var doc in task.Result.Documents)
                          {
                              doc.Reference.DeleteAsync();
                          }

                          Debug.Log("[FriendRequest] Accepted successfully");
                          onComplete?.Invoke(true);
                      }
                      else
                      {
                          Debug.LogError($"[FriendRequest] Accept failed: {task.Exception}");
                          onComplete?.Invoke(false);
                      }
                  });

                // notify lại
                var realtimeRef = FirebaseDatabase
            .GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app")
            .RootReference;
                realtimeRef.Child("friend_accept")
                           .Child(fromUserId)
                           .Child(toUserId)
                           .SetValueAsync(true);
            }
            else
            {
                onComplete?.Invoke(false);
            }
        });

        // notify lại
        var realtimeRef = FirebaseDatabase.GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app").RootReference;
        realtimeRef.Child("friend_accept")
                   .Child(fromUserId)
                   .Child(toUserId)
                   .SetValueAsync(true);
    }


    private const int MAX_SEND_PER_DAY = 3;

    public class SendBoosterException : Exception
    {
        public string ErrorCode;

        public SendBoosterException(string errorCode) : base(errorCode)
        {
            ErrorCode = errorCode;
        }
    }


    public void SendBooster(
        string fromUserId,
        string toUserId,
        string boosterName,
        int amount = 1,
        Action<bool> onComplete = null)
    {
        if (IsFirebaseInitialized && db == null)
            db = FirebaseFirestore.DefaultInstance;

        DocumentReference fromUserRef =
            db.Collection(COLLECTION_NAME).Document(fromUserId);

        DocumentReference toUserRef =
            db.Collection(COLLECTION_NAME).Document(toUserId);

        db.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot fromSnap =
                await transaction.GetSnapshotAsync(fromUserRef);

            DocumentSnapshot toSnap =
                await transaction.GetSnapshotAsync(toUserRef);

            if (!fromSnap.Exists || !toSnap.Exists)
                throw new SendBoosterException("USER_NOT_FOUND");

            // =====================================================
            // 🇻🇳 TIME VN
            // =====================================================

            DateTime vnNow = DateTime.UtcNow.AddHours(7);
            string today = vnNow.ToString("yyyyMMdd");

            string lastDate = "";

            if (fromSnap.ContainsField("LastSendBoosterDate"))
            {
                lastDate = fromSnap.GetValue<string>("LastSendBoosterDate");
            }

            int sendCount = 0;

            if (fromSnap.ContainsField("SendBoosterCount"))
            {
                sendCount = fromSnap.GetValue<int>("SendBoosterCount");
            }

            // reset count nếu sang ngày mới
            if (lastDate != today)
            {
                sendCount = 0;
            }

            if (sendCount >= MAX_SEND_PER_DAY)
            {
                throw new SendBoosterException("LIMIT_REACHED");
            }

            // =====================================================
            // ❤️ HEART
            // =====================================================

            if (boosterName == "Heart")
            {
                int fromHeart = fromSnap.GetValue<int>("Heart");
                int toHeart = toSnap.GetValue<int>("Heart");

                if (fromHeart < amount)
                {
                    throw new SendBoosterException("NOT_ENOUGH");
                }

                transaction.Update(fromUserRef, "Heart", fromHeart - amount);
                transaction.Update(toUserRef, "Heart", toHeart + amount);
            }
            else
            {
                // =====================================================
                // 🎁 BOOSTER
                // =====================================================

                List<Dictionary<string, object>> fromBoosters =
                    fromSnap.ContainsField("Boosters")
                    ? fromSnap.GetValue<List<Dictionary<string, object>>>("Boosters")
                    : new List<Dictionary<string, object>>();

                bool enough = false;

                foreach (var booster in fromBoosters)
                {
                    if (booster["name"].ToString() == boosterName)
                    {
                        int current = Convert.ToInt32(booster["count"]);

                        if (current >= amount)
                        {
                            booster["count"] = current - amount;
                            enough = true;
                        }

                        break;
                    }
                }

                if (!enough)
                {
                    throw new SendBoosterException("NOT_ENOUGH");
                }

                List<Dictionary<string, object>> toBoosters =
                    toSnap.ContainsField("Boosters")
                    ? toSnap.GetValue<List<Dictionary<string, object>>>("Boosters")
                    : new List<Dictionary<string, object>>();

                bool found = false;

                foreach (var booster in toBoosters)
                {
                    if (booster["name"].ToString() == boosterName)
                    {
                        int current = Convert.ToInt32(booster["count"]);
                        booster["count"] = current + amount;

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toBoosters.Add(new Dictionary<string, object>
                    {
                    { "name", boosterName },
                    { "count", amount }
                    });
                }

                transaction.Update(fromUserRef, "Boosters", fromBoosters);
                transaction.Update(toUserRef, "Boosters", toBoosters);
            }

            // =====================================================
            // ✅ UPDATE DAILY COUNT
            // =====================================================

            transaction.Update(fromUserRef, new Dictionary<string, object>
            {
            { "LastSendBoosterDate", today },
            { "SendBoosterCount", sendCount + 1 }
            });

            return true;
        })
        .ContinueWithOnMainThread(task =>
        {
            // =====================================================
            // ✅ SUCCESS
            // =====================================================

            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"[Send] {boosterName} x{amount} success");

                // realtime notify
                var realtimeRef = FirebaseDatabase
                    .GetInstance("https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app")
                    .RootReference;

                realtimeRef.Child("send_booster")
                           .Child(toUserId)
                           .Push()
                           .SetValueAsync(new Dictionary<string, object>
                           {
                           { "fromUserId", fromUserId },
                           { "boosterName", boosterName },
                           { "amount", amount }
                           });

                // update local sender
                if (fromUserId == CurrentUserId)
                {
                    if (boosterName == "Heart")
                    {
                        HeartManager.Instance.TryConsume(amount);
                    }
                    else
                    {
                        foreach (var booster in UserData.listBoosterCounters)
                        {
                            if (booster.name == boosterName)
                            {
                                booster.count -= amount;
                                break;
                            }
                        }
                        SaveDataManager.Save();
                    }
                }

                UIManager.Instance.NotifyContent(Loc.Get("send_success"));

                onComplete?.Invoke(true);

                return;
            }

            // =====================================================
            // ❌ ERROR
            // =====================================================

            Exception ex = task.Exception?
                .Flatten()
                .InnerException;

            if (ex is SendBoosterException sendEx)
            {
                switch (sendEx.ErrorCode)
                {
                    case "LIMIT_REACHED":
                        UIManager.Instance.NotifyContent(Loc.Get("gift_daily_limit_fmt", MAX_SEND_PER_DAY));
                        break;

                    case "NOT_ENOUGH":
                        UIManager.Instance.NotifyContent(Loc.Get("gift_not_enough"));
                        break;

                    case "USER_NOT_FOUND":
                        UIManager.Instance.NotifyContent(Loc.Get("user_not_found"));
                        break;

                    default:
                        UIManager.Instance.NotifyContent(Loc.Get("gift_daily_limit_fmt", MAX_SEND_PER_DAY));
                        break;
                }
            }
            else
            {
                UIManager.Instance.NotifyContent(Loc.Get("firebase_error"));
            }

            Debug.LogError($"[Send] Failed: {task.Exception}");

            onComplete?.Invoke(false);
        });
    }

    //login with google
    private Action<bool> _pendingOnComplete;

    public async Task LinkGoogleAccount(Action<bool> onComplete = null)
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!PlayerAccountService.Instance.IsSignedIn)
            {
                // ✅ Lưu callback lại để dùng sau
                _pendingOnComplete = onComplete;

                // ✅ Subscribe event TRƯỚC khi StartSignIn
                PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;

                // Mở browser cho user đăng nhập, sau đó return luôn
                // Phần còn lại xử lý trong OnPlayerAccountSignedIn
                await PlayerAccountService.Instance.StartSignInAsync();
                return;
            }

            // Nếu đã đăng nhập Unity Player Accounts rồi thì đi thẳng
            await SignInWithUnityAndSave(onComplete);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Google Link] {ex.GetType().Name}: {ex.Message}");
            UIManager.Instance.NotifyContent(Loc.Get("google_link_failed"));
            onComplete?.Invoke(false);
        }
    }

    private async void OnPlayerAccountSignedIn()
    {
        // ✅ Gỡ listener ngay để tránh bị gọi nhiều lần
        PlayerAccountService.Instance.SignedIn -= OnPlayerAccountSignedIn;

        await SignInWithUnityAndSave(_pendingOnComplete);
    }

    private async Task SignInWithUnityAndSave(Action<bool> onComplete)
    {
        try
        {
            string accessToken = PlayerAccountService.Instance.AccessToken;

            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("AccessToken is null after sign-in");

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);

            string unityPlayerId = AuthenticationService.Instance.PlayerId;
            string currentUserId = PlayerPrefs.GetString("PlayerID", "");

            if (string.IsNullOrEmpty(currentUserId))
                throw new Exception("No local PlayerID found");

            // =====================================================
            // 🔍 Tìm xem UnityPlayerId này đã được liên kết với account nào trên Firebase chưa
            // =====================================================
            if (db == null) db = FirebaseFirestore.DefaultInstance;

            QuerySnapshot existingQuery = await db.Collection(COLLECTION_NAME)
                .WhereEqualTo("UnityPlayerId", unityPlayerId)
                .GetSnapshotAsync();

            if (existingQuery.Count > 0)
            {
                // =====================================================
                // ✅ Đã có account trên Firebase → kéo data về local
                // =====================================================
                DocumentSnapshot existingDoc = existingQuery.Documents.First();
                Dictionary<string, object> cloudData = existingDoc.ToDictionary();
                string cloudUserId = existingDoc.Id;

                Debug.Log($"[Google Link] Found existing account on Firebase: {cloudUserId}");

                // Cập nhật PlayerID & PlayerName local
                string cloudUserName = cloudData.ContainsKey("Name") ? cloudData["Name"].ToString() : "Player" + cloudUserId;

                // Nếu account cloud khác với local hiện tại → dừng listener cũ, chuyển sang account cloud
                if (cloudUserId != currentUserId)
                {
                    StopListeningFriendRequest();

                    CurrentUserId = cloudUserId;
                    CurrentUserName = cloudUserName;
                    PlayerPrefs.SetString("PlayerID", cloudUserId);
                    PlayerPrefs.SetString("PlayerName", cloudUserName);
                }

                // Cập nhật UserData local từ Firebase
                if (cloudData.ContainsKey("Coin"))
                    UserData.coin = System.Convert.ToInt32(cloudData["Coin"]);

                if (cloudData.ContainsKey("Level"))
                    UserData.level = System.Convert.ToInt32(cloudData["Level"]);

                if (cloudData.ContainsKey("Heart"))
                {
                    int hearts = System.Convert.ToInt32(cloudData["Heart"]);
                    PlayerPrefs.SetInt("Hearts", hearts);
                    // if (HeartSystem.Instance != null)
                    //     HeartSystem.Instance.CurrentHearts = hearts;
                }

                // Cập nhật Boosters
                if (cloudData.ContainsKey("Boosters"))
                {
                    UserData.listBoosterCounters = new List<BoosterCounter>();
                    var boostersList = cloudData["Boosters"] as List<object>;
                    if (boostersList != null)
                    {
                        foreach (var item in boostersList)
                        {
                            var dict = item as Dictionary<string, object>;
                            if (dict != null)
                            {
                                UserData.listBoosterCounters.Add(new BoosterCounter
                                {
                                    name = dict.ContainsKey("name") ? dict["name"].ToString() : "",
                                    count = dict.ContainsKey("count") ? System.Convert.ToInt32(dict["count"]) : 0
                                });
                            }
                        }
                    }
                }

                PlayerPrefs.Save();

                // Lưu local file
                SaveDataManager.Save();

                // Khởi động lại listener với userId mới
                StartListeningFriendRequest(CurrentUserId);

                Debug.Log($"[Google Link] Data synced from Firebase! UserId: {cloudUserId}, Coin: {UserData.coin}, Level: {UserData.level}");
                UIManager.Instance.NotifyContent(Loc.Get("google_login_synced"));
                onComplete?.Invoke(true);
            }
            else
            {
                // =====================================================
                // 🆕 Chưa có account → liên kết account local hiện tại với Google
                // =====================================================
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "UnityPlayerId", unityPlayerId },
                    { "LoginType", "Google" },
                    { "GoogleLinkedAt", FieldValue.ServerTimestamp }
                };

                await db.Collection(COLLECTION_NAME)
                        .Document(currentUserId)
                        .SetAsync(updates, SetOptions.MergeAll);

                Debug.Log($"[Google Link] Linked current account! UnityPlayerId: {unityPlayerId}");
                UIManager.Instance.NotifyContent(Loc.Get("google_link_success"));
                onComplete?.Invoke(true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Google Link] {ex.GetType().Name}: {ex.Message}");
            UIManager.Instance.NotifyContent(Loc.Get("google_link_failed"));
            onComplete?.Invoke(false);
        }
    }

}
