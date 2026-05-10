// // File: Assets/Editor/LevelToolWindow.cs
// // Place this file under an Editor folder.

// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// #region NOTE
// /* 
//   ADDITIONAL FEATURES ADDED:
//   - Add Selected Objects from Scene (Assign type Slot/Block/Gate)
//   - Maintains full compatibility with your existing create/save/undo logic.
// */
// #endregion

// public class LevelToolWindow : EditorWindow
// {
//     // ===================== PREFAB LISTS =====================
//     public List<GameObject> slotHolderPrefabs = new List<GameObject>();
//     public List<GameObject> blockPrefabs = new List<GameObject>();
//     public List<GameObject> gatePrefabs = new List<GameObject>();

//     private int slotHolderIndex = 0;
//     private int blockIndex = 0;
//     private int gateIndex = 0;

//     // ===================== EDITOR DATA =====================
//     private List<SlotHolderData> slotList = new List<SlotHolderData>();
//     private List<BlockData> blockList = new List<BlockData>();
//     private List<GateData> gateList = new List<GateData>();

//     private Dictionary<string, GameObject> uidToObject = new Dictionary<string, GameObject>();
//     private Dictionary<string, Vector3> lastPositions = new Dictionary<string, Vector3>();

//     private Vector2 scrollPos;
//     private string newName = "NewObject";
//     private BlockColor blockColor = BlockColor.Red;
//     private MoveDir moveDir = MoveDir.NORMAL;
//     private List<GateColorInfo> gateColors = new List<GateColorInfo>();

//     // *** ADDED FEATURE — type for importing selected objects
//     private ObjectType importType = ObjectType.Slot;

//     private bool foldPrefabs = true;
//     private bool foldCreation = true;
//     private bool foldManagedList = true;

//     // ===== Menu =====
//     [MenuItem("Tools/Level Tool (Full)")]
//     public static void OpenWindow()
//     {
//         var w = GetWindow<LevelToolWindow>("Level Tool");
//         w.minSize = new Vector2(400, 300);
//     }

//     // ===== OnEnable / OnDisable =====
//     private void OnEnable()
//     {
//         EditorApplication.hierarchyChanged += OnHierarchyChanged;
//         Undo.postprocessModifications += OnPostprocessModifications;
//         Undo.undoRedoPerformed += OnUndoRedoPerformed;

//         ScanSceneForManagedObjects();
//     }

//     private void OnDisable()
//     {
//         EditorApplication.hierarchyChanged -= OnHierarchyChanged;
//         Undo.postprocessModifications -= OnPostprocessModifications;
//         Undo.undoRedoPerformed -= OnUndoRedoPerformed;
//     }

//     // ===== GUI =====
//     void OnGUI()
//     {
//         // Toolbar
//         EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//         if (GUILayout.Button("Rescan Scene", EditorStyles.toolbarButton))
//             ScanSceneForManagedObjects();

//         if (GUILayout.Button("Select All Managed", EditorStyles.toolbarButton))
//             Selection.objects = uidToObject.Values.Where(v => v != null).ToArray();

//         GUILayout.FlexibleSpace();

//         if (GUILayout.Button("Save To SO", EditorStyles.toolbarButton))
//             SaveToSO();


//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.Space();

//         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

//         // ==== PREFAB LIST ====
//         foldPrefabs = EditorGUILayout.Foldout(foldPrefabs, "Prefab Lists", true);
//         if (foldPrefabs)
//         {
//             EditorGUILayout.LabelField("Slot Holder Prefabs", EditorStyles.boldLabel);
//             DrawPrefabArray(slotHolderPrefabs);

//             EditorGUILayout.LabelField("Block Prefabs", EditorStyles.boldLabel);
//             DrawPrefabArray(blockPrefabs);

//             EditorGUILayout.LabelField("Gate Prefabs", EditorStyles.boldLabel);
//             DrawPrefabArray(gatePrefabs);

//             slotHolderIndex = EditorGUILayout.IntSlider("Slot Index", slotHolderIndex, 0, Mathf.Max(0, slotHolderPrefabs.Count - 1));
//             blockIndex = EditorGUILayout.IntSlider("Block Index", blockIndex, 0, Mathf.Max(0, blockPrefabs.Count - 1));
//             gateIndex = EditorGUILayout.IntSlider("Gate Index", gateIndex, 0, Mathf.Max(0, gatePrefabs.Count - 1));
//         }

//         EditorGUILayout.Space();

//         // ==== CREATION ====
//         foldCreation = EditorGUILayout.Foldout(foldCreation, "Create / Quick Settings", true);
//         if (foldCreation)
//         {
//             newName = EditorGUILayout.TextField("Name", newName);

//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("Create Slot Holder")) CreateSlotHolderImmediate();
//             if (GUILayout.Button("Create Block")) CreateBlockImmediate();
//             if (GUILayout.Button("Create Gate")) CreateGateImmediate();
//             EditorGUILayout.EndHorizontal();

//             blockColor = (BlockColor)EditorGUILayout.EnumPopup("Block Color", blockColor);
//             moveDir = (MoveDir)EditorGUILayout.EnumPopup("Move Direction", moveDir);


//             // Gate colors
//             EditorGUILayout.LabelField("Gate Colors", EditorStyles.boldLabel);
//             if (GUILayout.Button("Add Gate Color"))
//                 gateColors.Add(new GateColorInfo(BlockColor.Red, 1));

//             for (int i = 0; i < gateColors.Count; i++)
//             {
//                 EditorGUILayout.BeginHorizontal();
//                 gateColors[i].color = (BlockColor)EditorGUILayout.EnumPopup(gateColors[i].color);
//                 gateColors[i].capacity = EditorGUILayout.IntField(gateColors[i].capacity);
//                 if (GUILayout.Button("X", GUILayout.Width(25))) gateColors.RemoveAt(i);
//                 EditorGUILayout.EndHorizontal();
//             }

//             // *** ADDED FEATURE: Import Selected Objects
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("Import Object From Scene", EditorStyles.boldLabel);
//             importType = (ObjectType)EditorGUILayout.EnumPopup("Import As Type", importType);

//             if (GUILayout.Button("Add Selected Objects"))
//                 AddSelectedObjects();

//             EditorGUILayout.Space();
//         }

//         // ==== MANAGED LIST ====
//         foldManagedList = EditorGUILayout.Foldout(foldManagedList, $"Managed Objects (Total: {uidToObject.Count})", true);
//         if (foldManagedList)
//         {
//             DrawManagedSection("Slots", uid => IsType(uid, ObjectType.Slot), slotList.Select(s => s.name).ToList());
//             DrawManagedSection("Blocks", uid => IsType(uid, ObjectType.Block), blockList.Select(b => b.name).ToList());
//             DrawManagedSection("Gates", uid => IsType(uid, ObjectType.Gate), gateList.Select(g => g.name).ToList());
//         }

//         EditorGUILayout.EndScrollView();
//     }


//     // === Helper: Draw Prefab List ===
//     private void DrawPrefabArray(List<GameObject> list)
//     {
//         EditorGUILayout.BeginVertical("box");
//         if (GUILayout.Button("Add Prefab Slot")) list.Add(null);

//         for (int i = 0; i < list.Count; i++)
//         {
//             EditorGUILayout.BeginHorizontal();
//             list[i] = (GameObject)EditorGUILayout.ObjectField(list[i], typeof(GameObject), false);
//             if (GUILayout.Button("X", GUILayout.Width(25)))
//             {
//                 list.RemoveAt(i);
//                 i--;
//             }
//             EditorGUILayout.EndHorizontal();
//         }

//         EditorGUILayout.EndVertical();
//     }

//     // === Draw Managed Section ===
//     private void DrawManagedSection(string title, Func<string, bool> uidFilter, List<string> displayNames)
//     {
//         EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
//         EditorGUILayout.BeginVertical("box");

//         var uids = uidToObject.Keys.Where(uidFilter).ToList();

//         if (uids.Count == 0)
//             EditorGUILayout.LabelField("(none)");

//         foreach (string uid in uids)
//         {
//             if (!uidToObject.ContainsKey(uid)) continue;
//             GameObject go = uidToObject[uid];
//             if (go == null) continue;

//             EditorGUILayout.BeginHorizontal();
//             GUILayout.Label(GetTypeName(uid), GUILayout.Width(60));
//             GUILayout.Label(go.name, GUILayout.ExpandWidth(true));

//             if (GUILayout.Button("Select", GUILayout.Width(60)))
//             {
//                 Selection.activeGameObject = go;
//                 EditorGUIUtility.PingObject(go);
//             }

//             if (GUILayout.Button("Delete", GUILayout.Width(60)))
//                 DeleteManagedObject(go);

//             EditorGUILayout.EndHorizontal();
//         }

//         EditorGUILayout.EndVertical();
//     }

//     // ---------------------------------------------------------
//     // CREATE FROM PREFAB
//     // ---------------------------------------------------------

//     private void CreateSlotHolderImmediate()
//     {
//         if (!CheckIndex(slotHolderPrefabs, slotHolderIndex)) return;
//         GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(slotHolderPrefabs[slotHolderIndex], SceneManager.GetActiveScene());
//         Undo.RegisterCreatedObjectUndo(go, "Create SlotHolder");
//         RegisterNewInstance(go, ObjectType.Slot, slotHolderIndex);
//     }

//     private void CreateBlockImmediate()
//     {
//         if (!CheckIndex(blockPrefabs, blockIndex)) return;
//         GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefabs[blockIndex], SceneManager.GetActiveScene());
//         Undo.RegisterCreatedObjectUndo(go, "Create Block");
//         RegisterNewInstance(go, ObjectType.Block, blockIndex, blockColor);
//     }

//     private void CreateGateImmediate()
//     {
//         if (!CheckIndex(gatePrefabs, gateIndex)) return;
//         GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(gatePrefabs[gateIndex], SceneManager.GetActiveScene());
//         Undo.RegisterCreatedObjectUndo(go, "Create Gate");
//         RegisterNewInstance(go, ObjectType.Gate, gateIndex, BlockColor.Red, new List<GateColorInfo>(gateColors));
//     }


//     // ---------------------------------------------------------
//     // *** ADDED FEATURE — IMPORT SELECTED OBJECTS
//     // ---------------------------------------------------------
//     private void AddSelectedObjects()
//     {
//         var selected = Selection.gameObjects;
//         if (selected == null || selected.Length == 0)
//         {
//             Debug.LogWarning("No GameObject selected.");
//             return;
//         }

//         foreach (var go in selected)
//         {
//             if (go == null) continue;

//             // nếu object đã quản lý → bỏ qua
//             if (go.GetComponent<LevelToolTag>() != null)
//             {
//                 Debug.Log($"'{go.name}' is already managed.");
//                 continue;
//             }

//             Undo.RecordObject(go, "Add Managed Object");

//             var tag = Undo.AddComponent<LevelToolTag>(go);
//             tag.uid = Guid.NewGuid().ToString();
//             tag.type = importType;
//             tag.prefabIndex = -1;

//             uidToObject[tag.uid] = go;
//             lastPositions[tag.uid] = go.transform.position;

//             switch (importType)
//             {
//                 case ObjectType.Slot:
//                     slotList.Add(new SlotHolderData(go.name, go.transform.position, go.transform.rotation.eulerAngles));
//                     break;

//                 case ObjectType.Block:
//                     blockList.Add(new BlockData(go.name, go.transform.position, blockColor, go.transform.rotation.eulerAngles, moveDir));
//                     break;

//                 case ObjectType.Gate:
//                     gateList.Add(new GateData(go.name, go.transform.position, go.transform.rotation.eulerAngles)
//                     {
//                         colorOutputs = new List<GateColorInfo>()
//                     });
//                     break;
//             }
//         }

//         EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
//         Repaint();
//     }


//     // ---------------------------------------------------------
//     // REGISTER NEW INSTANCE
//     // ---------------------------------------------------------

//     private void RegisterNewInstance(GameObject go, ObjectType type, int prefabIndex, BlockColor color = BlockColor.Red, List<GateColorInfo> gateColorList = null)
//     {
//         string finalName = MakeUniqueName(newName);
//         go.name = finalName;

//         var tag = go.GetComponent<LevelToolTag>();
//         if (tag == null)
//             tag = Undo.AddComponent<LevelToolTag>(go);

//         tag.uid = Guid.NewGuid().ToString();
//         tag.type = type;
//         tag.prefabIndex = prefabIndex;
//         tag.color = color;
//         tag.gateColors = gateColorList != null ? new List<GateColorInfo>(gateColorList) : new List<GateColorInfo>();

//         uidToObject[tag.uid] = go;
//         lastPositions[tag.uid] = go.transform.position;

//         switch (type)
//         {
//             case ObjectType.Slot:
//                 slotList.Add(new SlotHolderData(finalName, go.transform.position, go.transform.rotation.eulerAngles));
//                 break;

//             case ObjectType.Block:
//                 blockList.Add(new BlockData(finalName, go.transform.position, color, go.transform.rotation.eulerAngles, moveDir));
//                 break;

//             case ObjectType.Gate:
//                 gateList.Add(new GateData(finalName, go.transform.position, go.transform.rotation.eulerAngles)
//                 {
//                     colorOutputs = new List<GateColorInfo>(tag.gateColors)
//                 });
//                 break;
//         }

//         EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
//         Selection.activeGameObject = go;
//     }

//     // ---------------------------------------------------------
//     // SCAN SCENE
//     // ---------------------------------------------------------
//     private void ScanSceneForManagedObjects()
//     {
//         uidToObject.Clear();
//         slotList.Clear();
//         blockList.Clear();
//         gateList.Clear();
//         lastPositions.Clear();

//         var all = Resources.FindObjectsOfTypeAll<GameObject>()
//             .Where(go => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)))
//             .ToArray();

//         foreach (var go in all)
//         {
//             var tag = go.GetComponent<LevelToolTag>();
//             if (tag == null) continue;

//             uidToObject[tag.uid] = go;
//             lastPositions[tag.uid] = go.transform.position;

//             string name = go.name;

//             switch (tag.type)
//             {
//                 case ObjectType.Slot:
//                     slotList.Add(new SlotHolderData(name, go.transform.position, go.transform.rotation.eulerAngles));
//                     break;

//                 case ObjectType.Block:
//                     blockList.Add(new BlockData(name, go.transform.position, tag.color, go.transform.rotation.eulerAngles, moveDir));
//                     break;

//                 case ObjectType.Gate:
//                     gateList.Add(new GateData(name, go.transform.position, go.transform.rotation.eulerAngles)
//                     {
//                         colorOutputs = new List<GateColorInfo>(tag.gateColors)
//                     });
//                     break;
//             }
//         }
//     }


//     // ---------------------------------------------------------
//     // DELETE
//     // ---------------------------------------------------------
//     private void DeleteManagedObject(GameObject go)
//     {
//         if (go == null) return;

//         var tag = go.GetComponent<LevelToolTag>();
//         string uid = tag != null ? tag.uid : null;

//         Undo.DestroyObjectImmediate(go);

//         if (!string.IsNullOrEmpty(uid))
//         {
//             uidToObject.Remove(uid);
//             lastPositions.Remove(uid);
//             ScanSceneForManagedObjects();
//         }
//     }

//     // ---------------------------------------------------------
//     // TRANSFORM UPDATES
//     // ---------------------------------------------------------
//     private UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
//     {
//         foreach (var mod in modifications)
//         {
//             var comp = mod.currentValue.target as Component;
//             if (comp == null) continue;

//             var go = comp.gameObject;
//             var tag = go.GetComponent<LevelToolTag>();
//             if (tag == null) continue;

//             UpdatePositionForTag(tag.uid, go.transform.position);
//         }
//         return modifications;
//     }

//     private void UpdatePositionForTag(string uid, Vector3 newPos)
//     {
//         if (!uidToObject.ContainsKey(uid)) return;
//         GameObject go = uidToObject[uid];
//         if (go == null) return;

//         string name = go.name;

//         var s = slotList.Find(x => x.name == name);
//         if (s != null) { s.position = newPos; return; }

//         var b = blockList.Find(x => x.name == name);
//         if (b != null) { b.position = newPos; return; }

//         var g = gateList.Find(x => x.name == name);
//         if (g != null) { g.position = newPos; return; }
//     }


//     // ---------------------------------------------------------
//     // UNDO / REDO / HIERARCHY
//     // ---------------------------------------------------------
//     private void OnHierarchyChanged()
//     {
//         ScanSceneForManagedObjects();
//         Repaint();
//     }

//     private void OnUndoRedoPerformed()
//     {
//         ScanSceneForManagedObjects();
//         Repaint();
//     }


//     // ---------------------------------------------------------
//     // SAVE TO SO
//     // ---------------------------------------------------------
//     private void SaveToSO()
//     {
//         LevelData data = ScriptableObject.CreateInstance<LevelData>();

//         data.slotHolders = new List<SlotHolderData>(slotList);
//         data.blocks = new List<BlockData>(blockList);
//         data.gates = new List<GateData>(gateList);

//         string path = EditorUtility.SaveFilePanelInProject("Save Level Data", "LevelData", "asset", "Choose save location");
//         if (string.IsNullOrEmpty(path)) return;

//         AssetDatabase.CreateAsset(data, path);
//         AssetDatabase.SaveAssets();

//         EditorUtility.DisplayDialog("Saved", "LevelData saved to: " + path, "OK");
//     }


//     // ---------------------------------------------------------
//     // UTILS
//     // ---------------------------------------------------------
//     private bool CheckIndex(List<GameObject> list, int index)
//     {
//         return list != null && list.Count > 0 && index >= 0 && index < list.Count && list[index] != null;
//     }

//     private bool IsType(string uid, ObjectType type)
//     {
//         if (!uidToObject.ContainsKey(uid)) return false;
//         var tag = uidToObject[uid].GetComponent<LevelToolTag>();
//         if (tag == null) return false;
//         return tag.type == type;
//     }

//     private string MakeUniqueName(string baseName)
//     {
//         var existing = new HashSet<string>(uidToObject.Values.Select(v => v.name));
//         if (!existing.Contains(baseName)) return baseName;

//         int i = 1;
//         string name;
//         do { name = baseName + "_" + i++; }
//         while (existing.Contains(name));

//         return name;
//     }

//     private string GetTypeName(string uid)
//     {
//         var tag = uidToObject[uid].GetComponent<LevelToolTag>();
//         return tag != null ? tag.type.ToString() : "";
//     }


// }

// // ===== TAG COMPONENT =====
// [ExecuteAlways]
// [DisallowMultipleComponent]
// public class LevelToolTag : MonoBehaviour
// {
//     public string uid;
//     public ObjectType type;
//     public int prefabIndex;
//     public BlockColor color;
//     public List<GateColorInfo> gateColors = new List<GateColorInfo>();
// }

// // ===== SUPPORT TYPES =====
// public enum ObjectType { Slot, Block, Gate }

// // (You already have these in your project)
