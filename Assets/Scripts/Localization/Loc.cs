using System;
using System.Collections.Generic;
using UnityEngine;

public enum Language { EN, VI }

public static class Loc
{
    public static Language Current { get; private set; } = Language.EN;
    public static event Action OnLanguageChanged;

    static Dictionary<string, string> _table = new Dictionary<string, string>();

    public static void Init(Language lang)
    {
        SetLanguage(lang, raiseEvent: false);
    }

    public static void SetLanguage(Language lang, bool raiseEvent = true)
    {
        Current = lang;
        string fileName = lang == Language.VI ? "vi" : "en";
        var asset = Resources.Load<TextAsset>("Localization/" + fileName);
        if (asset == null)
        {
            Debug.LogError($"[Loc] Missing Resources/Localization/{fileName}.json — strings will fall through to keys.");
            _table.Clear();
        }
        else
        {
            _table = Parse(asset.text);
        }
        if (raiseEvent) OnLanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return _table.TryGetValue(key, out var v) ? v : key;
    }

    public static string Get(string key, params object[] args)
    {
        var fmt = Get(key);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }

    static Dictionary<string, string> Parse(string json)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        var table = JsonUtility.FromJson<LocTable>(json);
        if (table != null && table.entries != null)
        {
            foreach (var e in table.entries)
            {
                if (!string.IsNullOrEmpty(e.k)) dict[e.k] = e.v ?? string.Empty;
            }
        }
        return dict;
    }

    [Serializable] class LocTable { public List<LocEntry> entries; }
    [Serializable] class LocEntry { public string k; public string v; }
}
