using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLoadingGamePlay : PopupUI
{
    [SerializeField] private List<Sprite> spriteLoadings;
    [SerializeField] private Image imageBg;
    [SerializeField] private TextMeshProUGUI TextLoading;

    private TMP_TextInfo textInfo;
    private float waveAmplitude = 5f;   // Biên độ lượn sóng (cao/thấp của sóng)
    private float waveFrequency = 2f;   // Tần số sóng (nhanh/chậm)
    private float waveSpeed = 5f;       // Tốc độ di chuyển của sóng

    private void Start()
    {
        int rand = Random.Range(0, spriteLoadings.Count);
        imageBg.sprite = spriteLoadings[rand];
        TextLoading.text = "Loading";
        StartCoroutine(TextLoadingAnimation());
    }

    private IEnumerator TextLoadingAnimation()
    {
        TextLoading.ForceMeshUpdate();
        textInfo = TextLoading.textInfo;

        Vector3[] vertices;
        TMP_CharacterInfo charInfo;
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * waveSpeed;
            TextLoading.ForceMeshUpdate();

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int vertexIndex = charInfo.vertexIndex;
                int materialIndex = charInfo.materialReferenceIndex;
                vertices = textInfo.meshInfo[materialIndex].vertices;

                // Tính độ dịch chuyển theo sóng sin
                float offsetY = Mathf.Sin(time + i * waveFrequency) * waveAmplitude;

                // Dịch chuyển 4 vertex của từng ký tự
                Vector3 offset = new Vector3(0, offsetY, 0);
                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            // Cập nhật lại mesh
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                TextLoading.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }
}
