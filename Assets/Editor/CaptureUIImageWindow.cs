using UnityEngine;
using UnityEditor;
using System.IO;

public class ImageCombinerWindow : EditorWindow
{
    private Texture2D[] images;
    private int columns = 3;
    private string outputFileName = "CombinedImage.png";

    [MenuItem("Tools/Image Combiner")]
    public static void ShowWindow()
    {
        GetWindow<ImageCombinerWindow>("Image Combiner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ghép nhiều ảnh thành 1 ảnh", EditorStyles.boldLabel);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty imagesProp = so.FindProperty("images");
        EditorGUILayout.PropertyField(imagesProp, true);
        so.ApplyModifiedProperties();

        columns = EditorGUILayout.IntField("Số cột:", columns);
        outputFileName = EditorGUILayout.TextField("Tên file xuất:", outputFileName);

        if (GUILayout.Button("Tạo Ảnh"))
        {
            if (images == null || images.Length == 0)
            {
                Debug.LogError("Chưa chọn ảnh!");
                return;
            }

            CombineImages();
        }
    }

    private void CombineImages()
    {
        int total = images.Length;
        int rows = Mathf.CeilToInt((float)total / columns);

        int width = images[0].width;
        int height = images[0].height;

        Texture2D result = new Texture2D(columns * width, rows * height, TextureFormat.RGBA32, false);

        for (int i = 0; i < total; i++)
        {
            Texture2D img = images[i];
            int x = (i % columns) * width;
            int y = (rows - 1 - (i / columns)) * height;

            Color[] pixels = img.GetPixels();
            result.SetPixels(x, y, width, height, pixels);
        }

        result.Apply();

        string path = EditorUtility.SaveFilePanel("Lưu ảnh kết quả", "Assets", outputFileName, "png");
        if (path.Length != 0)
        {
            File.WriteAllBytes(path, result.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log("Xuất ảnh thành công tại: " + path);
        }
    }
}
