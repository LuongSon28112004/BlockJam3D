using UnityEngine;
using UnityEditor;
using System.IO;

public class ConvertTextureToPNG
{
    [MenuItem("Tools/Convert To PNG", false, 2000)]
    private static void ConvertToPNG()
    {
        Object obj = Selection.activeObject;
        string assetPath = AssetDatabase.GetAssetPath(obj);

        Texture2D texture = null;

        // Nếu chọn Texture
        if (obj is Texture2D)
        {
            texture = (Texture2D)obj;
        }
        // Nếu chọn Sprite
        else if (obj is Sprite sprite)
        {
            texture = sprite.texture;
            assetPath = AssetDatabase.GetAssetPath(texture); // lấy path của texture gốc
        }
        else
        {
            Debug.LogError("Object được chọn không phải Texture hoặc Sprite!");
            return;
        }

        Texture2D readable = GetReadableTexture(texture);

        byte[] pngData = readable.EncodeToPNG();

        string directory = Path.GetDirectoryName(assetPath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(assetPath);

        // thêm hậu tố để không ghi đè file gốc
        string newPath = $"{directory}/{fileNameWithoutExt}_CONVERT.png";

        File.WriteAllBytes(newPath, pngData);

        AssetDatabase.Refresh();
        Debug.Log("✔ Convert thành công: " + newPath);
    }


    private static Texture2D GetReadableTexture(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }
}
