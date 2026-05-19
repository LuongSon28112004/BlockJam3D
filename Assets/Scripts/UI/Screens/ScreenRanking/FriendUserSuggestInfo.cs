using UnityEngine;
using UnityEngine.UI;

public class FriendUserSuggestInfo : MonoBehaviour
{
    [SerializeField] private Text txtName;
    [SerializeField] private Text txtId;
    private string userId;
    [SerializeField] private Button buttonAddFriend;

    public void SetData(string id, string name)
    {
        txtName.text = name;
        txtId.text = id;
        userId = id;
        buttonAddFriend.onClick.RemoveAllListeners();
        buttonAddFriend.onClick.AddListener(OnClickAddFriend);
    }

    private void OnClickAddFriend()
    {
        string myId = PlayerPrefs.GetString("PlayerID", null);
        // Handle add friend logic here
        UserDataFirebaseManager.Instance.SendFriendRequest(myId, userId, success =>
        {
            if (success)
            {
                UIManager.Instance.NotifyContent(Loc.Get("friend_request_sent"));
            }
            else
            {
                UIManager.Instance.NotifyContent(Loc.Get("friend_request_send_error"));
            }
        });
    }
}
