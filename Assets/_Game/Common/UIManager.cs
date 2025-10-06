using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;

#if UNITY_EDITOR
namespace RPGFantasy.EditorClass
{
    using UnityEditor;

    /*[CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Get References"))
            {
                UIManager u = target as UIManager;
                u.canvasScaler = u.GetComponentsInChildren<CanvasScaler>(true);
                // u.popups = FindObjectsOfType<PopupUI>(true);
                PopupUI[] popups = Resources.LoadAll<PopupUI>("UI/Popups/");
                for (int i = 0; i < popups.Length; i++)
                {
                    string nname = popups[i].GetType().Name;
                    Debug.Log(nname);
                    popups[i].gameObject.name = nname;
                    string pa = AssetDatabase.GetAssetPath(popups[i].gameObject);
                    AssetDatabase.RenameAsset(pa, nname);
                    AssetDatabase.SaveAssetIfDirty(popups[i].gameObject);
                }
                ScreenUI[] screens = Resources.LoadAll<ScreenUI>("UI/Screens/");
                for (int i = 0; i < screens.Length; i++)
                {
                    string nname = screens[i].GetType().Name;
                    Debug.Log(nname);
                    screens[i].gameObject.name = nname;
                    string pa = AssetDatabase.GetAssetPath(screens[i].gameObject);
                    AssetDatabase.RenameAsset(pa, nname);
                    AssetDatabase.SaveAssetIfDirty(screens[i].gameObject);
                }
                EditorUtility.SetDirty(u);
            }
        }
    }*/
}
#endif
public class UIManager : master.Singleton<UIManager>
{
    public Canvas canvas;
    public Canvas canvasScreen;
    public GameObject blockerUI;
    public Image fadeImage;
    public RectTransform screenHolder;
    public RectTransform popupHolder;
    [SerializeField] NotifyPanel notifyPanel;
    [SerializeField] CanvasScaler[] canvasScaler;
    List<PopupUI> listPopupCached = new List<PopupUI>();
    List<PopupUI> listPopupExist = new List<PopupUI>();
    List<ScreenUI> listScreenCached = new List<ScreenUI>();
    List<ScreenUI> listScreenExist = new List<ScreenUI>();
    public List<PopupUI> ListPopupCached => listPopupCached;
    public ScreenUI CurrentScreen { get; private set; }
    public PopupUI CurrentPopup { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        blockerUI.SetActive(false);
        ResetResolution();
        /*  for (int i = 0; i < listScreenCached.Count; i++)
          {
              listScreenCached[i].Initialize(this);
          }
          for (int i = 0; i < listPopupCached.Count; i++)
          {
              listPopupCached[i].Initialize(this);
          }*/
        /*listPopupExist = new List<PopupUI>(listPopupCached);
        listScreenExist = new List<ScreenUI>(listScreenCached);*/
        PopupUI.OnDestroyPopup += OnPopupDestroyed;
        ScreenUI.OnDestroyScreen += OnDestroyScreen;
    }

    public void ResetResolution()
    {
        //if (SdkUtil.isFold())
        //{
        //    foreach (var c in canvasScaler)
        //    {
        //        c.matchWidthOrHeight = 1f;
        //        c.referenceResolution = new Vector2(1080, 2200);
        //    }
        //}
        //else if (SdkUtil.isiPad())
        //{
        //    foreach (var c in canvasScaler)
        //    {
        //        c.matchWidthOrHeight = 1f;
        //        c.referenceResolution = new Vector2(1080, 2200);
        //    }
        //}
        //else
        //{
        //    foreach (var c in canvasScaler)
        //    {
        //        c.matchWidthOrHeight = 0f;
        //        c.referenceResolution = new Vector2(1080, 1920);
        //    }
        //}
        
    }

    public static float CalculateObjectScaleFactor(float matchWidthOrHeight, int referenceWidth = 1080, int referenceHeight = 1920)
    {
        float logWidth  = Mathf.Log((float)Screen.width / referenceWidth, 2);
        float logHeight = Mathf.Log((float)Screen.height / referenceHeight, 2);

        float lerp = Mathf.Lerp(logWidth, logHeight, matchWidthOrHeight);
        return Mathf.Pow(2, lerp); // scale factor cho GameObject world-space
    }
    
    public bool ShowBreakAds(int typeShowOnPlaying)
    {
        //if (Application.internetReachability != NetworkReachability.NotReachable && !AdsHelper.isRemoveAds(0))
        //{
        //    AudioManager.Instance.SetCacheAudio();
        //    var ss = AdsHelper.Instance.showFull("break_ads", GameRes.GetLevel(), -1, 0, 0, true, false, true,
        //        state =>
        //        {
        //            if (state == AD_State.AD_REWARD_OK || state == AD_State.AD_CLOSE || state == AD_State.AD_CLOSE2 ||
        //                state == AD_State.AD_SHOW_MISS_CB || state == AD_State.AD_SHOW_FAIL ||
        //                state == AD_State.AD_SHOW_FAIL2)
        //            {
        //                AudioManager.Instance.ResetAudio();
        //            }
        //        });
        //    return true;
        //}

        return false;
    }

    public void SetCanvasScale(float val)
    {
        canvasScreen.GetComponent<CanvasScaler>().matchWidthOrHeight = val;
        Canvas.ForceUpdateCanvases();
    }

    public void SetResolution(int width, int height)
    {
        canvasScreen.GetComponent<CanvasScaler>().referenceResolution = new Vector2(width, height);
        Canvas.ForceUpdateCanvases();
    }

    public void NotifyContent(string content, string key = "", float number = 0)
    {
        notifyPanel.ShowNotify(content, key, number);
    }

    public void NotifyContent2(string content, string key = "", string objFormat = "")
    {
        notifyPanel.ShowNotify(content, key, objFormat);
    }

    public void OnPopupDestroyed(PopupUI obj)
    {
        if (listPopupCached.Contains(obj))
        {
            listPopupCached.Remove(obj);
        }

        listPopupExist.Remove(obj);
        Destroy(obj.gameObject);
    }

    public void OnDestroyScreen(ScreenUI screen)
    {
        if (listScreenCached.Contains(screen))
        {
            listScreenCached.Remove(screen);
        }

        if (listScreenExist.Contains(screen))
        {
            listScreenExist.Remove(screen);
        }

        Destroy(screen.gameObject);
    }

    public T ShowScreen<T>() where T : ScreenUI
    {
        if (CurrentScreen)
        {
            CurrentScreen.Deactive();
        }

        for (int i = 0; i < listScreenCached.Count; i++)
        {
            if (listScreenCached[i] is T)
            {
                CurrentScreen = listScreenCached[i];
                listScreenCached[i].Active();
                listScreenCached[i].transform.SetAsLastSibling();
                SetFirstScreen();
                return listScreenCached[i].GetComponent<T>();
            }
        }

        T screen = CreateScreen<T>();
        CurrentScreen = screen;
        screen.Active();
        screen.transform.SetAsLastSibling();
        SetFirstScreen();
        return screen;
    }

    private T CreateScreen<T>() where T : ScreenUI
    {
        string screenName = typeof(T).Name;
        T screen = Instantiate(Resources.Load<T>("UI/Screens/" + screenName), screenHolder);
        listScreenExist.Add(screen);
        if (screen.isCache)
        {
            listScreenCached.Add(screen);
        }

        screen.Initialize(this);
        return screen;
    }

    public T GetScreen<T>() where T : ScreenUI
    {
        T screen = default;
        for (int i = 0; i < listScreenExist.Count; i++)
        {
            if (listScreenExist[i] is T)
            {
                screen = listScreenExist[i].GetComponent<T>();
                return screen;
            }
        }

        screen = CreateScreen<T>();
        return screen;
    }

    public T GetScreenActive<T>() where T : ScreenUI
    {
        T screen = default;
        for (int i = 0; i < listScreenExist.Count; i++)
        {
            if (listScreenExist[i] is T)
            {
                screen = listScreenExist[i].GetComponent<T>();
                return screen;
            }
        }

        return screen;
    }

    public T ShowPopup<T>(System.Action onClose) where T : PopupUI
    {
        for (int i = 0; i < listPopupCached.Count; i++)
        {
            if (listPopupCached[i] is T)
            {
                listPopupCached[i].Show(onClose);
                listPopupCached[i].transform.SetAsLastSibling();
                CurrentPopup = listPopupCached[i];
                SetFirstScreen();
                return listPopupCached[i].GetComponent<T>();
            }
        }

        T popup = CreatePopup<T>();
        popup.Show(onClose);
        popup.transform.SetAsLastSibling();
        CurrentPopup = popup;
        SetFirstScreen();
        return popup;
    }

    private T CreatePopup<T>() where T : PopupUI
    {
        string popupName = typeof(T).Name;
        T popup = Instantiate(Resources.Load<T>("UI/Popups/" + popupName), popupHolder);
        listPopupExist.Add(popup);
        if (popup.isCache)
        {
            listPopupCached.Add(popup);
        }

        popup.Setup();
        return popup;
    }

    public T GetPopup<T>() where T : PopupUI
    {
        T popup = default;
        for (int i = 0; i < listPopupCached.Count; i++)
        {
            if (listPopupCached[i] is T)
            {
                popup = listPopupCached[i].GetComponent<T>();
                return popup;
            }
        }

        for (int i = 0; i < listPopupExist.Count; i++)
        {
            if (listPopupExist[i] is T)
            {
                popup = listPopupExist[i].GetComponent<T>();
                return popup;
            }
        }

        popup = CreatePopup<T>();
        return popup;
    }

    public void HideAllPopup()
    {
        for (int i = 0; i < listPopupCached.Count; i++)
        {
            if (listPopupCached[i])
            {
                listPopupCached[i].Hide();
            }
        }

        for (int i = 0; i < listPopupExist.Count; i++)
        {
            if (listPopupExist[i])
            {
                listPopupExist[i].Hide();
            }
        }
    }

    public T GetPopupActive<T>() where T : PopupUI
    {
        T popup = default;
        for (int i = 0; i < listPopupCached.Count; i++)
        {
            if (listPopupCached[i] is T)
            {
                popup = listPopupCached[i].GetComponent<T>();
                return popup;
            }
        }

        for (int i = 0; i < listPopupExist.Count; i++)
        {
            if (listPopupExist[i] is T)
            {
                popup = listPopupExist[i].GetComponent<T>();
                return popup;
            }
        }

        return popup;
    }

    public bool HasPopupShowing()
    {
        foreach (var item in listPopupExist)
        {
            if (item.isShowing) return true;
        }

        return false;
    }

    public PopupUI GetPopupCached<T>() where T : PopupUI
    {
        T popup = default;
        for (int i = 0; i < listPopupCached.Count; i++)
        {
            if (listPopupCached[i] is T)
            {
                popup = listPopupCached[i].GetComponent<T>();
                return popup;
            }
        }

        return popup;
    }

    public void AddPopupCache(PopupUI popup)
    {
        listPopupCached.Add(popup);
    }

    public void ClearPopupCache(PopupUI popup)
    {
        listPopupExist.Remove(popup);
        listPopupCached.Remove(popup);
    }

    public void SetFirstScreen()
    {
        //if(!SDKManager.Instance) return;
        var str = "";
        if (CurrentPopup && CurrentPopup.gameObject.activeInHierarchy)
        {
            str = CurrentPopup.gameObject.name;
        }
        else if (CurrentScreen)
        {
            str = CurrentScreen.gameObject.name;
        }
 
        if (string.IsNullOrEmpty(str))
        {
            //SDKManager.Instance.currPlacement = "default";
            return;
        }

        var f = PascalToSnake(str);
        f = f.Replace("(Clone)", "").Replace("(clone)", "");
        //SDKManager.Instance.currPlacement = f;
        //Debug.Log($"aaaaaaaaaaaaa={f}");
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public static string PascalToSnake(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        var result = Regex.Replace(input, @"([A-Z]+)([A-Z][a-z])", "$1_$2");
        result = Regex.Replace(result, @"([a-z0-9])([A-Z])", "$1_$2");
        result = Regex.Replace(result, @"([A-Z])([a-z])_([A-Z])", "$1$2$3");
        return result.ToLower();
    }
}

public static class CanvasPositioningExtensions
{
    public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        var viewportPosition = camera.WorldToViewportPoint(worldPosition);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
    {
        var viewportPosition = new Vector3(screenPosition.x / Screen.width, screenPosition.y / Screen.height, 0);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
    {
        var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
        var canvasRect = canvas.GetComponent<RectTransform>();
        var scale = canvasRect.sizeDelta;
        return Vector3.Scale(centerBasedViewPortPosition, scale);
    }
}