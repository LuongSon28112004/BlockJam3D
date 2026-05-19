using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LocText : MonoBehaviour
{
    [SerializeField] private string key;

    private Text _uiText;
    private TMP_Text _tmpText;

    public string Key
    {
        get => key;
        set { key = value; Refresh(); }
    }

    private void Awake()
    {
        _uiText = GetComponent<Text>();
        _tmpText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Loc.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Loc.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (string.IsNullOrEmpty(key)) return;
        string value = Loc.Get(key);
        if (_uiText != null) _uiText.text = value;
        if (_tmpText != null) _tmpText.text = value;
    }
}
