using UnityEditor;
using UnityEngine;

public class EnableMeshReadWrite : EditorWindow
{
    [MenuItem("Tools/Enable ReadWrite For Selected Mesh")]
    static void EnableReadWrite()
    {
        foreach (Object obj in Selection.objects)
        {
            Mesh mesh = obj as Mesh;
            if (mesh == null)
            {
                Debug.LogWarning("Selected object is not a Mesh: " + obj.name);
                continue;
            }

            SerializedObject so = new SerializedObject(mesh);
            SerializedProperty readableProp = so.FindProperty("m_IsReadable");

            if (readableProp != null)
            {
                readableProp.boolValue = true;
                so.ApplyModifiedProperties();
                Debug.Log("Enabled Read/Write for mesh: " + mesh.name);
            }
            else
            {
                Debug.LogError("Could not find m_IsReadable property on: " + mesh.name);
            }
        }
    }
}
