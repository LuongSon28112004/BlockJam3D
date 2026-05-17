# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.0.60f1** (URP) mobile match/sort puzzle game. Target platforms: Android (60 FPS) and Editor (120 FPS) — see `GameManager.Start`. Comments and a number of identifiers are in Vietnamese; preserve language when editing nearby code.

There is no test suite, no CI, no build scripts. Building, playing, and editing are done through the Unity Editor. There is no CLI workflow — open `BlockJam3D.sln`/the project in Unity 6000.0.60f1 to do anything meaningful.

## Architecture

### Scene flow & GameManager state machine
`Assets/Scripts/GameManager/GameManager.cs` is the entry point and drives the game via a `GameState` enum (`Loading → Menu → GamePlay → Win/Lose/Pause`). State transitions go through `ChangeState(...)` coroutine, which loads scenes (`UIMain`, `GamePlay`) via `LoadSceneAndWait` and then asks `UIManager` to show the right `ScreenUI`/`PopupUI`. Three real scenes: `Init.unity` (bootstraps singletons), `UIMain.unity` (menus), `GamePlay.unity` (board). `Test.unity` and `TestFireBase.unity` are sandboxes.

### Singletons & DDOL
All managers derive from helpers in `Assets/_Game/Common/Singleton.cs` (`namespace master`): `Singleton<T>` (scene-scoped, `FindObjectOfType`), `SingletonDDOL<T>` (DontDestroyOnLoad, the bootstrap kind), `SingletonAutoCreate<T>`, `SingletonSO<T>`. `GameManager`, `AddressableManager`, `CustomeEventSystem`, `AudioManager` are DDOL; `LevelManager`, `UIManager`, `BoosterCtrl` are scene singletons recreated per scene load.

### Addressables-driven content (critical)
`Assets/Scripts/Addressables/AddressableManager.cs` preloads all gameplay assets at startup by label:
- `"Wall"`, `"Container"`, `"Block"`, `"GridSpot"` → prefab cache, keyed by GameObject name.
- `"Level"` → `LevelData` SOs. Levels with names like `Level_3(board_1)` are grouped by base name (`Level_3`) and sorted by the `board_N` index — each group is one level's rounds.

`GetLevelGroup("Level_" + GameManager.Level)` returns the rounds. Looking up assets by literal string name (e.g. `GetPrefab("BlockRed")`) is the normal pattern — `LevelData.prefabNames[y * width + x]` stores the addressable name per board cell.

When adding a prefab/level you MUST add the matching Addressable label or it won't load at runtime. Editor tool: `Assets/Scripts/Editor/LevelEditorWindow.cs`.

### LevelManager & rounds
`Assets/Scripts/LevelManager/LevelManager.cs` orchestrates each level. A level = a list of `LevelData` rounds; `Round` is the index. `NextRoundLevel()` is the round transition (pools old objects, increments `Round`, calls `BoardCtrl.LoadLevel`, re-runs `AddTutorial`). `BoosterCtrl.IsBusy` is set true during transitions to lock booster input. Hard-coded: levels 1 and 2 trigger tutorial popups; `Round > 2` ends the level.

Sub-controllers (siblings on the same scene root):
- `BoardCtrl` (`Board/`) — grid layout, cells, walls, containers, spawn logic; `grid[,]`, `IsWall[,]`, `gridContainerSpot[,]` mirror the level data.
- `CellPlayCtrl` (`CellPlay/`) — the play tray cells that receive picked blocks (match-3 happens here).
- `BoosterCtrl` (`Booster/`) — four booster types in subfolders: `BoosterUndo`, `BoosterAdd`, `BoosterShuffle`, `BoosterMagnet`. Each has a `*Pos` companion for placement preview.
- `FindingPath` (`Finding/`) — pathfinding for movement from board to tray.
- `TutorialCtrl` (`Tutorial/`) — `TutorialType` enum (`Click`, `Order`, `Pipe`, `Match_3`...).
- `GridSpot/` — spawners that drop new blocks onto the board.

### Event bus
`Assets/Scripts/CustomeEvent/CustomeEventSystem.cs` is the project-wide event hub (Action<T> fields + matching `Invoke` methods). Cross-system signals (`ChangeRound`, `ChangeCoin`, `ActiveBooster`, `CheckMatch_3`, `TutorialPos`, etc.) flow through here rather than direct references. Subscribe in `OnEnable`, unsubscribe in `OnDisable`.

### UI framework
`UIManager` (`_Game/Common/UIManager.cs`) manages `ScreenUI` (full-screen, one at a time) and `PopupUI` (stackable overlays). Prefabs live in `Assets/Resources/UI/Popups/` and `Assets/Resources/UI/Screens/` (Resources.LoadAll-based caching). Use `UIManager.Instance.ShowPopup<T>(...)`, `ShowScreen<T>()`, `HideAllPopup()`. Concrete screens/popups are in `Assets/Scripts/UI/{Popups,Screens}/`.

### Pooling
`Assets/Scripts/ObjPool/`: `BaseObjectPool` + `BlockItemSpawner` / `WallItemSpawner`. `LevelManager.AddPool()` returns objects to the pool at round transitions before re-spawning.

### Save & user data
`Assets/Scripts/UserData/UserData.cs` is a static class (coin, level, booster counts) — in-memory game state. `SaveDataManager` writes/reads `Application.persistentDataPath/UserData.json` with `JsonUtility`. Default booster inventory (2 each of Undo/Add/Shuffle/Magnet) is seeded by `Load()` when no save file exists. Call `SaveDataManager.Save()` after mutating `UserData.*`.

### Firebase
EDM4U-managed (`ExternalDependencyManager/`, `google-services.json`, `google-services-desktop.json`). Used for Firestore-backed user data and leaderboard (`Assets/Scenes/FireBaseInit.cs`, `UserDataFirebaseManager.cs`, `Assets/_Game/Leaderboard/`). Firebase init runs in `TestFireBase.unity`; integrate into the main flow only when needed.

## Conventions

- ScriptableObjects live in `Assets/Scripts/SO/` (definitions) with instances under `Assets/LevelData/`, `Assets/BoosterData/`.
- `LevelData.prefabNames[y * width + x]` — row-major, y is the outer index. `LevelData.gsp` is the spawn-point list with `(x, y, spawnCount, type)`.
- Booster name conventions on prefabs (case-insensitive substrings) are used by `LevelData.ShufflePrefabs` to decide what NOT to shuffle: anything containing `wall`, `container`, `gsp`, or `b` (barrel) is fixed in place.
- Avoid editing `Library/`, `Temp/`, `Logs/`, `UserSettings/`, generated `.csproj`/`.sln` — Unity owns those.
- DOTween is used heavily for animation (`using DG.Tweening`). The third-party shiny UI effect lives at `Assets/SRCGame/ShinyEffectForUGUI`.
