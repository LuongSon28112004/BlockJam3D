using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

//Only Blockies with an open path can be tapped
//Blockies can be collected in any order
//Blockies come out of pipes
public enum TutorialMode
{
    GamePlay,
    Mission,
}

public enum TutorialType
{
    Click,
    Order,
    Pipe
}

public class PopupTutorial : PopupUI
{
    private static string Tutorial_Click = "Only Blockies with an open path can be tapped";
    private static string Tutorial_Order = "Blockies can be collected in any order";
    private static string Tutorial_Pipe = "Blockies come out of pipes";
    [SerializeField] private TutorialMode currentTutorial;
    [SerializeField] private TutorialType currentTutorialType;
    [SerializeField] private Sprite iconHandPrefabs;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI match_3_Color_Text;
    [SerializeField] private TextMeshProUGUI Tutorial_Text;

    private Coroutine waveCoroutine;

    private void OnEnable()
    {
        CustomeEventSystem.Instance.TutorialPosAction += TutorialShow;
        CustomeEventSystem.Instance.ShowTextMatch_3_Action += ShowOrHideTextMatch_3;
        CustomeEventSystem.Instance.ChangeTextTutorialAction += ChangeText;
        if (match_3_Color_Text != null)
            StartWaveEffect();
    }

    private void OnDisable()
    {
        CustomeEventSystem.Instance.TutorialPosAction -= TutorialShow;
        CustomeEventSystem.Instance.ShowTextMatch_3_Action -= ShowOrHideTextMatch_3;
        CustomeEventSystem.Instance.ChangeTextTutorialAction -= ChangeText;
        if (waveCoroutine != null)
            StopCoroutine(waveCoroutine);
    }

    public void TutorialShow(TutorialMode tutorialMode, Vector3 pos)
    {
        currentTutorial = tutorialMode;
        switch (currentTutorial)
        {
            case TutorialMode.GamePlay:
                ShowTutorialGamePlay(pos);
                break;
            case TutorialMode.Mission:
                break;
        }
    }

    private void ShowTutorialGamePlay(Vector3 pos)
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        Transform oldHand = transform.Find("HandIcon");
        if (oldHand != null)
            Destroy(oldHand.gameObject);

        if (pos == new Vector3(-1000, -1000)) return;

        GameObject handObj = new GameObject("HandIcon");
        handObj.transform.SetParent(transform, false);

        Image handImage = handObj.AddComponent<Image>();
        handImage.sprite = iconHandPrefabs;
        handImage.raycastTarget = false;
        handImage.SetNativeSize();

        RectTransform rectTransform = handObj.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(120, 120);

        Vector2 screenPos = Camera.main.WorldToScreenPoint(pos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out Vector2 localPos
        );

        rectTransform.anchoredPosition = localPos - new Vector2(-50, 50);

        rectTransform.localScale = Vector3.one * 1.1f;
        rectTransform.DOScale(1f, 0.3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    // Show and Hide Text Match_3 
    private void ShowOrHideTextMatch_3(bool isShow)
    {
        match_3_Color_Text.gameObject.SetActive(isShow);
    }

    // Change Text Tutorial
    private void ChangeText(TutorialType tutorialType)
    {
        currentTutorialType = tutorialType;
        switch (currentTutorialType)
        {
            case TutorialType.Click:
                Tutorial_Text.text = Tutorial_Click.ToString();
                break;
            case TutorialType.Order:
                Tutorial_Text.text = Tutorial_Order.ToString();
                break;
            case TutorialType.Pipe:
                Tutorial_Text.text = Tutorial_Pipe.ToString();
                break;
        }
    }


    // ------------------- Hiệu ứng sóng chữ -------------------
    private void StartWaveEffect()
    {
        if (waveCoroutine != null)
            StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(WaveTextEffect());
    }

    private IEnumerator WaveTextEffect()
    {
        TMP_Text text = match_3_Color_Text;
        text.ForceMeshUpdate();

        float time = 0f;

        Color brown = new Color(0.6f, 0.3f, 0.1f);
        Color green = new Color(0.1f, 0.9f, 0.3f);

        while (true)
        {
            time += Time.deltaTime * 2f;

            // Nếu sin > 0 → xanh, ngược lại → nâu
            bool isGreen = Mathf.Sin(time * 2f) > 0f;
            text.color = isGreen ? green : brown;

            // Giữ hiệu ứng sóng y như cũ (nếu bạn muốn)
            text.ForceMeshUpdate();
            TMP_TextInfo textInfo = text.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                int materialIndex = charInfo.materialReferenceIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                // Wave nhảy chữ
                float wave = Mathf.Sin(time * 3f + i * 0.5f) * 5f;
                Vector3 offset = new Vector3(0, wave, 0);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            // Update lại mesh
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                text.UpdateGeometry(meshInfo.mesh, i);
            }

            yield return null;
        }
    }

}
