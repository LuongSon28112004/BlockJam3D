using UnityEngine;
using UnityEngine.UI;

public class PopupSettingsUIMain : PopupUI
{
    [SerializeField] private Button buttonSignInWithGoogle;

    private void Awake()
    {
        if (buttonSignInWithGoogle == null)
        {
            Transform t = FindDeep(transform, "SignInWithGoogle");
            if (t != null)
            {
                buttonSignInWithGoogle = t.GetComponent<Button>();
                if (buttonSignInWithGoogle == null) buttonSignInWithGoogle = t.gameObject.AddComponent<Button>();
            }
        }

        if (buttonSignInWithGoogle != null)
            buttonSignInWithGoogle.onClick.AddListener(OnSignInWithGoogleClicked);
    }

    private async void OnSignInWithGoogleClicked()
    {
        await UserDataFirebaseManager.Instance
            .LinkGoogleAccount((res) =>
            {
                if (res) UIManager.Instance.NotifyContent("Login Success");
                else UIManager.Instance.NotifyContent("Login Failed");
            });
    }


    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
