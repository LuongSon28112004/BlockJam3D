using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class PopupSettingsUIMain : PopupUI
{
    [SerializeField] private Button buttonSignInWithGoogle;
    [SerializeField] private TMP_Text buttonSignInWithGoogleLabel;
    [SerializeField] private GameObject googleIcon;

    [Header("Sound")]
    [SerializeField] private Button buttonSound;
    [SerializeField] private Image iconSound;
    [SerializeField] private Sprite spriteSoundOn;
    [SerializeField] private Sprite spriteSoundOff;

    [Header("Vibration")]
    [SerializeField] private Button buttonVibration;
    [SerializeField] private Image iconVibration;
    [SerializeField] private Sprite spriteVibrationOn;
    [SerializeField] private Sprite spriteVibrationOff;

    [Header("Language")]
    [SerializeField] private Button buttonEn;
    [SerializeField] private Button buttonVi;
    // iconEn / iconVi đã được thay thế bằng background Image của chính nút (tự resolve qua GetComponent<Image>()).
    [SerializeField] private Image iconEn;
    [SerializeField] private Image iconVi;

    private static readonly Color OFF_TINT = new Color(0.45f, 0.45f, 0.45f, 1f);

    public override void Show(System.Action onClose = null)
    {
        base.Show(onClose);
        // Popup được cache, Awake chỉ chạy 1 lần. Mỗi lần show phải resync trạng thái login & ngôn ngữ.
        RefreshLanguageButtons();
        RefreshLoginButton();
    }

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

        AutoDiscoverToggles();
        if (buttonSound != null) buttonSound.onClick.AddListener(OnToggleSound);
        if (buttonVibration != null) buttonVibration.onClick.AddListener(OnToggleVibration);
        if (buttonEn != null) buttonEn.onClick.AddListener(OnPickEn);
        if (buttonVi != null) buttonVi.onClick.AddListener(OnPickVi);

        RefreshIcons();
        RefreshLanguageButtons();
        RefreshLoginButton();
    }

    private void AutoDiscoverToggles()
    {
        if (buttonSound == null || iconSound == null)
        {
            Transform t = FindDeep(transform, "ButtonSound");
            if (t != null)
            {
                if (buttonSound == null)
                {
                    buttonSound = t.GetComponent<Button>();
                    if (buttonSound == null) buttonSound = t.gameObject.AddComponent<Button>();
                }
                if (iconSound == null)
                {
                    iconSound = t.GetComponentInChildren<Image>(true);
                }
            }
        }
        if (buttonVibration == null || iconVibration == null)
        {
            Transform t = FindDeep(transform, "ButtonVibrate");
            if (t != null)
            {
                if (buttonVibration == null)
                {
                    buttonVibration = t.GetComponent<Button>();
                    if (buttonVibration == null) buttonVibration = t.gameObject.AddComponent<Button>();
                }
                if (iconVibration == null)
                {
                    iconVibration = t.GetComponentInChildren<Image>(true);
                }
            }
        }
        if (buttonEn == null || iconEn == null)
        {
            Transform t = FindDeep(transform, "ButtonEn");
            if (t != null)
            {
                if (buttonEn == null)
                {
                    buttonEn = t.GetComponent<Button>();
                    if (buttonEn == null) buttonEn = t.gameObject.AddComponent<Button>();
                }
                // iconEn = chính background Image của ButtonEn (không còn icon riêng).
                if (iconEn == null) iconEn = buttonEn != null ? buttonEn.GetComponent<Image>() : t.GetComponentInChildren<Image>(true);
            }
        }
        if (buttonVi == null || iconVi == null)
        {
            Transform t = FindDeep(transform, "ButtonVi");
            if (t != null)
            {
                if (buttonVi == null)
                {
                    buttonVi = t.GetComponent<Button>();
                    if (buttonVi == null) buttonVi = t.gameObject.AddComponent<Button>();
                }
                // iconVi = chính background Image của ButtonVi (không còn icon riêng).
                if (iconVi == null) iconVi = buttonVi != null ? buttonVi.GetComponent<Image>() : t.GetComponentInChildren<Image>(true);
            }
        }
    }

    private void OnToggleSound()
    {
        AudioManager.AudioSoundSetting = !AudioManager.AudioSoundSetting;
        AudioManager.AudioMusicSetting = AudioManager.AudioSoundSetting;
        AudioManager.Instance.FixVolumeSFX();
        AudioManager.Instance.FixVolumeMusic();
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        RefreshIcons();
    }

    private void OnToggleVibration()
    {
        AudioManager.AudioVibrateSetting = !AudioManager.AudioVibrateSetting;
        if (AudioManager.AudioVibrateSetting) AudioManager.Instance.PlayVibrate();
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        if (iconSound != null)
        {
            if (spriteSoundOn != null && spriteSoundOff != null)
                iconSound.sprite = AudioManager.AudioSoundSetting ? spriteSoundOn : spriteSoundOff;
            iconSound.color = AudioManager.AudioSoundSetting ? Color.white : OFF_TINT;
        }
        if (iconVibration != null)
        {
            if (spriteVibrationOn != null && spriteVibrationOff != null)
                iconVibration.sprite = AudioManager.AudioVibrateSetting ? spriteVibrationOn : spriteVibrationOff;
            iconVibration.color = AudioManager.AudioVibrateSetting ? Color.white : OFF_TINT;
        }
    }

    private void OnPickEn() { ChangeLanguage(Language.EN); }
    private void OnPickVi() { ChangeLanguage(Language.VI); }

    private void ChangeLanguage(Language lang)
    {
        if (GameManager.Instance != null && GameManager.Instance.gameState != GameState.Menu) return;
        if (Loc.Current == lang) return;

        Loc.SetLanguage(lang);
        UserData.language = lang == Language.VI ? "vi" : "en";
        SaveDataManager.Save();
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        RefreshLanguageButtons();
        // Đồng bộ lại label nút login/logout theo ngôn ngữ mới.
        RefreshLoginButton();
    }

    private void RefreshLanguageButtons()
    {
        // Tô màu background của chính nút EN/VI: trắng = đang chọn, OFF_TINT = không chọn.
        Image bgEn = iconEn != null ? iconEn : (buttonEn != null ? buttonEn.GetComponent<Image>() : null);
        Image bgVi = iconVi != null ? iconVi : (buttonVi != null ? buttonVi.GetComponent<Image>() : null);
        if (bgEn != null) bgEn.color = Loc.Current == Language.EN ? Color.white : OFF_TINT;
        if (bgVi != null) bgVi.color = Loc.Current == Language.VI ? Color.white : OFF_TINT;
    }

    private bool IsLoggedIn
    {
        get
        {
            try
            {
                return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
            }
            catch
            {
                // Unity Services chưa init xong -> chưa thể đăng nhập.
                return false;
            }
        }
    }

    private void RefreshLoginButton()
    {
        if (buttonSignInWithGoogle == null) return;

        buttonSignInWithGoogle.onClick.RemoveAllListeners();

        bool loggedIn = IsLoggedIn;
        if (buttonSignInWithGoogleLabel != null)
        {
            buttonSignInWithGoogleLabel.text = loggedIn
                ? Loc.Get("settings_logout")
                : Loc.Get("ui_sign_in_with_google");
        }
        //if (googleIcon != null) googleIcon.SetActive(!loggedIn);

        if (loggedIn) buttonSignInWithGoogle.onClick.AddListener(OnLogoutClicked);
        else buttonSignInWithGoogle.onClick.AddListener(OnSignInWithGoogleClicked);
    }

    private void OnLogoutClicked()
    {
        if (UserDataFirebaseManager.Instance != null) UserDataFirebaseManager.Instance.SignOutUnityPlayer();
        if (UIManager.Instance != null) UIManager.Instance.NotifyContent(Loc.Get("settings_logged_out"));
        RefreshLoginButton();
    }

    private async void OnSignInWithGoogleClicked()
    {
        await UserDataFirebaseManager.Instance
            .LinkGoogleAccount((res) =>
            {
                if (res)
                {
                    UIManager.Instance.NotifyContent(Loc.Get("login_success"));
                    RefreshLoginButton();
                }
                else UIManager.Instance.NotifyContent(Loc.Get("login_failed"));
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
