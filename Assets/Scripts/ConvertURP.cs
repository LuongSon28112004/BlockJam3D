using UnityEngine;
using UnityEditor;
using System.IO;

public class ConvertURPMaterials : EditorWindow
{
    [MenuItem("Tools/Convert URP to Standard")]
    public static void ConvertMaterials()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
                continue;

            string shaderName = mat.shader.name;

            // Kiểm tra nếu là shader của URP
            if (shaderName.Contains("Universal Render Pipeline") ||
                shaderName.Contains("URP") ||
                shaderName.Contains("Lit"))
            {
                mat.shader = Shader.Find("Standard");
                convertedCount++;

                // Giữ lại texture và màu
                if (mat.HasProperty("_BaseMap") && mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", mat.GetTexture("_BaseMap"));
                if (mat.HasProperty("_BaseColor") && mat.HasProperty("_Color"))
                    mat.SetColor("_Color", mat.GetColor("_BaseColor"));

                EditorUtility.SetDirty(mat);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoàn tất", $"Đã chuyển {convertedCount} materials sang Standard Shader!", "OK");
    }
}
