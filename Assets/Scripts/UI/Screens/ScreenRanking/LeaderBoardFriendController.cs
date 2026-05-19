using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardFriendController : MonoBehaviour
{
    [Header("Reference")]
    private LeaderBoardManager leaderBoardManager;
    [SerializeField] private GameObject contentFriend;
    [SerializeField] private GameObject contentFriendRequest;
    [SerializeField] private GameObject Line;
    [SerializeField] private GameObject TitleFriendRequest;
    [Header("Prefab")]
    [SerializeField] private FriendUserInfor friendUserInforPrefab;
    [SerializeField] private FriendUserRequestInfo friendUserRequestInfoPrefab;
    [Header("Button")]
    [SerializeField] private Button buttonFriend;
    [SerializeField] private Button buttonAddFriend;
    [SerializeField] private Button buttonCopy;
    [SerializeField] private Sprite spriteButtonFriendSelected;
    [SerializeField] private Sprite spriteButtonFriendNormal;
    [Header("Layout Panel")]
    [SerializeField] private GameObject panelAddFriend;
    [SerializeField] private GameObject panelFriendList;
    [SerializeField] private GameObject LoadingPanel;
    [Header("Search Friends")]
    [SerializeField] private InputField inputSearchFriend;
    [SerializeField] private GameObject contentSearchFriend;
    [SerializeField] private FriendUserSuggestInfo friendUserSuggestInfoPrefab;
    [SerializeField] private Button buttonSearchFriend;
    [SerializeField] private Button buttonClearSearch;
    [SerializeField] private Text txtMyId;
    private void Start()
    {
        //remove old listener
        buttonFriend.onClick.RemoveAllListeners();
        buttonAddFriend.onClick.RemoveAllListeners();
        buttonSearchFriend.onClick.RemoveAllListeners();
        buttonClearSearch.onClick.RemoveAllListeners();
        buttonCopy.onClick.RemoveAllListeners();
        buttonFriend.onClick.AddListener(OnClickFriend);
        buttonAddFriend.onClick.AddListener(OnClickAddFriend);
        buttonSearchFriend.onClick.AddListener(OnClickSearchFriend);
        buttonClearSearch.onClick.AddListener(OnClickClearSearch);
        buttonCopy.onClick.AddListener(OnClickCopy);
    }

    private void OnClickCopy()
    {
        string myId = PlayerPrefs.GetString("PlayerID", null);
        if (!string.IsNullOrEmpty(myId))
        {
            GUIUtility.systemCopyBuffer = myId;
            UIManager.Instance.NotifyContent(Loc.Get("id_copied_to_clipboard"));
        }
        else
        {
            UIManager.Instance.NotifyContent(Loc.Get("id_not_found"));
        }
    }

    private void OnClickClearSearch()
    {
        inputSearchFriend.text = "";
    }

    private void OnClickSearchFriend()
    {
        if (string.IsNullOrEmpty(inputSearchFriend.text))
        {
            UIManager.Instance.NotifyContent(Loc.Get("enter_search_keyword"));
            // Clear old search results
            foreach (Transform child in contentSearchFriend.transform)
            {
                Destroy(child.gameObject);
            }
            return;
        }

        LoadingPanel.SetActive(true);

        UserDataFirebaseManager.Instance.SearchUsersByIdPrefix(inputSearchFriend.text, users =>
        {
            // Clear old search results
            foreach (Transform child in contentSearchFriend.transform)
            {
                Destroy(child.gameObject);
            }
            // loại tôi ra khỏi kết quả tìm kiếm
            string currentUserId = PlayerPrefs.GetString("PlayerID", null);
            users.RemoveAll(user => user.ContainsKey("Id") && user["Id"].ToString() == currentUserId);
            // loại bạn bè ra khỏi kết quả tìm kiếm
            UserDataFirebaseManager.Instance.GetFriendsList(currentUserId, friends =>
            {
                if (friends != null)
                {
                    var friendIds = new HashSet<string>();
                    foreach (var friend in friends)
                    {
                        if (friend.ContainsKey("Id"))
                        {
                            friendIds.Add(friend["Id"].ToString());
                        }
                    }
                    users.RemoveAll(user => user.ContainsKey("Id") && friendIds.Contains(user["Id"].ToString()));
                }
                if (users == null || users.Count == 0)
                {
                    UIManager.Instance.NotifyContent(Loc.Get("no_users_found"));
                    LoadingPanel.SetActive(false);
                    return;
                }


                // Hiển thị kết quả tìm kiếm
                foreach (var user in users)
                {
                    string userId = user.ContainsKey("Id") ? user["Id"].ToString() : "Unknown";
                    string userName = user.ContainsKey("Name") ? user["Name"].ToString() : "Unknown";

                    GameObject item = Instantiate(friendUserSuggestInfoPrefab.gameObject, contentSearchFriend.transform);
                    FriendUserSuggestInfo ui = item.GetComponent<FriendUserSuggestInfo>();
                    ui.SetData(userId, userName);
                    item.SetActive(true);

                }
                LoadingPanel.SetActive(false);
            });
        });
    }

    private void OnClickAddFriend()
    {
        // Clear old search results
        foreach (Transform child in contentSearchFriend.transform)
        {
            Destroy(child.gameObject);
        }

        GameObject itemFriend = buttonFriend.transform.Find("Button").gameObject;
        itemFriend.GetComponent<Image>().sprite = spriteButtonFriendNormal;
        GameObject itemAddFriend = buttonAddFriend.transform.Find("Button").gameObject;
        itemAddFriend.GetComponent<Image>().sprite = spriteButtonFriendSelected;

        // clear input field
        inputSearchFriend.text = "";
        panelAddFriend.SetActive(true);
        panelFriendList.SetActive(false);
        string currentUserId = PlayerPrefs.GetString("PlayerID", null);

        if (!string.IsNullOrEmpty(currentUserId))
        {
            txtMyId.text = $"{currentUserId}";
        }
    }

    private void OnClickFriend()
    {
        LoadListFriend();
        GameObject itemFriend = buttonFriend.transform.Find("Button").gameObject;
        itemFriend.GetComponent<Image>().sprite = spriteButtonFriendSelected;
        GameObject itemAddFriend = buttonAddFriend.transform.Find("Button").gameObject;
        itemAddFriend.GetComponent<Image>().sprite = spriteButtonFriendNormal;
        panelAddFriend.SetActive(false);
        panelFriendList.SetActive(true);
    }

    public void LoadListFriend()
    {
        string currentUserId = PlayerPrefs.GetString("PlayerID", null);
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Current user ID not found in PlayerPrefs.");
            return;
        }

        // destroy old list
        foreach (Transform child in contentFriend.transform)
        {
            Destroy(child.gameObject);
        }

        LoadingPanel.SetActive(true);

        UserDataFirebaseManager.Instance.GetFriendsList(currentUserId, friends =>
        {
            if (friends == null)
            {
                Debug.LogError("Failed to load friends list.");
                return;
            }

            // Clear old list
            foreach (Transform child in contentFriend.transform)
            {
                Destroy(child.gameObject);
            }

            if (friends.Count == 0)
            {
                LoadingPanel.SetActive(false);
                UIManager.Instance.NotifyContent(Loc.Get("no_friends"));
                return;
            }

            //add tôi vào list bạn bè
            UserDataFirebaseManager.Instance.GetUserData(currentUserId, currentUser =>
            {
                if (currentUser != null)
                {
                    friends.Add(currentUser);
                }

                LoadingPanel.SetActive(false);
                //sort theo level giảm dần
                friends.Sort((a, b) =>
                {
                    int levelA = a.ContainsKey("Level") ? Convert.ToInt32(a["Level"]) : 1;
                    int levelB = b.ContainsKey("Level") ? Convert.ToInt32(b["Level"]) : 1;
                    return levelB.CompareTo(levelA);
                });

                // Create new UI for each friend
                int rank = 1;
                foreach (var friend in friends)
                {
                    string friendName = friend.ContainsKey("Name") ? friend["Name"].ToString() : "Unknown";
                    int friendLevel = friend.ContainsKey("Level") ? Convert.ToInt32(friend["Level"]) : 1;
                    string id = friend.ContainsKey("Id") ? friend["Id"].ToString() : "Unknown";

                    GameObject item = Instantiate(friendUserInforPrefab.gameObject, contentFriend.transform);
                    FriendUserInfor ui = item.GetComponent<FriendUserInfor>();
                    ui.SetData(rank, friendName, friendLevel, id);
                    rank++;
                    item.SetActive(true);
                }
            });

        });
    }

    public void LoadListFriendRequest()
    {
        string currentUserId = PlayerPrefs.GetString("PlayerID", null);
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Current user ID not found in PlayerPrefs.");
            return;
        }

        UserDataFirebaseManager.Instance.GetMyFriendRequests(currentUserId, friendRequests =>
        {
            if (friendRequests == null)
            {
                Debug.LogError("Failed to load friend requests.");
                return;
            }

            // Clear old list
            foreach (Transform child in contentFriendRequest.transform)
            {
                Destroy(child.gameObject);
            }

            contentFriendRequest.SetActive(friendRequests.Count > 0);
            Line.SetActive(friendRequests.Count > 0);
            TitleFriendRequest.SetActive(friendRequests.Count > 0);

            // Create new UI for each friend request
            foreach (var request in friendRequests)
            {
                string fromUserId = request.ContainsKey("FromUserId") ? request["FromUserId"].ToString() : "Unknown";
                string fromUserName = request.ContainsKey("FromUserName") ? request["FromUserName"].ToString() : "Unknown";

                GameObject item = Instantiate(friendUserRequestInfoPrefab.gameObject, contentFriendRequest.transform);
                FriendUserRequestInfo ui = item.GetComponent<FriendUserRequestInfo>();
                ui.SetData(fromUserId, fromUserName);
                item.SetActive(true);
            }
        });
    }

    public void ClearContent()
    {
        foreach (Transform child in contentFriend.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
