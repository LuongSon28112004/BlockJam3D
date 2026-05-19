using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupSettings : PopupUI
{
    [SerializeField] private Button quitLevel;

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

    void Awake()
    {
        quitLevel.onClick.AddListener(BackToMain);
        AutoDiscoverToggles();
        if (buttonSound != null) buttonSound.onClick.AddListener(OnToggleSound);
        if (buttonVibration != null) buttonVibration.onClick.AddListener(OnToggleVibration);
        RefreshIcons();
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

    private static readonly Color OFF_TINT = new Color(0.45f, 0.45f, 0.45f, 1f);

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

    private void BackToMain()
    {
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        if (HeartManager.Instance != null) HeartManager.Instance.TryConsume(1);
        GameManager.Instance.BackToMenu();
    }
}
