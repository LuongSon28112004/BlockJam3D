# DOCUMENTATION — BlockJam3D

> Tài liệu tổng quan về kiến trúc, luồng hoạt động và các thành phần chính của dự án **BlockJam3D** — game match/sort puzzle 3D di động viết trên Unity 6000.0.60f1 (URP).
>
> Mục tiêu: cung cấp wireframe luồng game đầy đủ, mô tả chi tiết chức năng từng component và mối quan hệ giữa chúng.

---

## MỤC LỤC

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Cấu trúc thư mục](#2-cấu-trúc-thư-mục)
3. [Wireframe luồng tổng thể (game flow)](#3-wireframe-luồng-tổng-thể)
4. [Bootstrap & vòng đời ứng dụng](#4-bootstrap--vòng-đời-ứng-dụng)
5. [GameManager — máy trạng thái](#5-gamemanager--máy-trạng-thái)
6. [Hệ thống Singleton](#6-hệ-thống-singleton)
7. [Sơ đồ Scene flow](#7-sơ-đồ-scene-flow)
8. [AddressableManager — quản lý tài nguyên](#8-addressablemanager)
9. [CustomeEventSystem — event bus](#9-customeeventsystem--event-bus)
10. [AudioManager](#10-audiomanager)
11. [LevelManager & các sub-controller](#11-levelmanager--các-sub-controller)
12. [BoardCtrl — bàn chơi](#12-boardctrl--bàn-chơi)
13. [CellPlayCtrl — khay nhận block](#13-cellplayctrl--khay-nhận-block)
14. [BoosterCtrl & 4 Booster](#14-boosterctrl--4-booster)
15. [FindingPath — pathfinding BFS](#15-findingpath--bfs)
16. [TutorialCtrl](#16-tutorialctrl)
17. [GridSpot — máng sinh block](#17-gridspot--máng-sinh-block)
18. [Object Pool](#18-object-pool)
19. [LevelData & Level Editor](#19-leveldata--level-editor)
20. [UI Framework — UIManager](#20-ui-framework--uimanager)
21. [Screens — wireframe từng màn hình](#21-screens--wireframe-từng-màn-hình)
22. [Popups — wireframe từng popup](#22-popups--wireframe-từng-popup)
23. [Hệ thống Tim (HeartManager)](#23-hệ-thống-tim-heartmanager)
24. [UserData & SaveDataManager](#24-userdata--savedatamanager)
25. [Firebase Integration](#25-firebase-integration)
26. [Leaderboard & Friend System](#26-leaderboard--friend-system)
27. [Sơ đồ quan hệ tổng thể](#27-sơ-đồ-quan-hệ-tổng-thể)

---

## 1. Tổng quan dự án

**BlockJam3D** là game match/sort puzzle 3D, lối chơi cốt lõi:

- Bàn chơi (LeaderBoard) chứa các khối (block) thuộc 7 loại (Blue/Brown/Green/Magenta/Red/Purple/Yellow), tường (Wall), container và máng sinh block (GridSpot).
- Người chơi click vào block để khối đó di chuyển theo đường BFS xuống **khay chơi (CellPlay)** chứa tối đa 7 ô.
- Khi 3+ block cùng loại được gom liền nhau trên khay → kích hoạt **Match-3** → các khối tan biến.
- Mỗi level gồm **3 round** (board_1, board_2, board_3). Hoàn tất cả 3 round = **Win**. Nếu khay đầy 7 mà không match được = **Lose**.
- 4 **Booster** hỗ trợ: **Undo, Add, Shuffle, Magnet**.
- Hệ thống **Tim (Heart)** giới hạn lượt chơi (tối đa 5 tim, 1 tim/10 phút).
- **Firebase Firestore + Realtime Database** đồng bộ data, leaderboard, hệ thống bạn bè và quà tặng booster.

**Build target:**
- Android: 60 FPS
- Editor: 120 FPS
- Cài tại `GameManager.Start()`

---

## 2. Cấu trúc thư mục

```
Assets/
├── _Game/Common/                 — UIManager, Singleton, AudioManager, ScreenUI, PopupUI
├── AddressableAssetsData/        — Cấu hình Addressables (groups, labels)
│   ├── AssetGroups/
│   │   ├── AssetBlock.asset      → label "Block"
│   │   ├── AssetContainer.asset  → label "Container"
│   │   ├── Wall.asset            → label "Wall"
│   │   ├── GridSpot.asset        → label "GridSpot"
│   │   └── LevelSO.asset         → label "Level"
│   └── Schemas/, Android/, Windows/
├── Audio/, Animation/, Material/, Mesh/, Texture/, Sprite/
├── BoosterData/                  — 4 SO: Undo, Add, Shuffle, Magnet
├── Editor/                       — Editor tools (ConvertURP, …)
├── Firebase/                     — SDK Firebase + EDM4U
├── Font/                         — TMP font, FarmHeroes-Bold, Inter-SemiBold
├── LevelData/                    — 10 thư mục Level_1 … Level_10, mỗi level 3 board
│   └── Level_X/Level_X(board_N).asset
├── Plugins/, ExternalDependencyManager/, ThirdParty/
├── Resources/
│   └── UI/
│       ├── Screens/              — Prefab 6 màn hình
│       └── Popups/               — Prefab 10 popup
├── Scenes/                       — 5 scene + 2 script Firebase
│   ├── Init.unity                — Scene bootstrap
│   ├── UIMain.unity              — Menu chính
│   ├── GamePlay.unity            — Gameplay
│   ├── Test.unity                — Sandbox
│   ├── TestFireBase.unity        — Sandbox Firebase
│   ├── FireBaseInit.cs           — Test Firestore
│   └── UserDataFirebaseManager.cs— Firebase manager chính (1.4k dòng)
├── Scripts/
│   ├── Addressables/             — AddressableManager
│   ├── CustomeEvent/             — CustomeEventSystem (event bus)
│   ├── Editor/                   — LevelEditorWindow
│   ├── FitCamera/                — CameraManager
│   ├── GameManager/              — GameManager, HeartManager
│   ├── LevelManager/
│   │   ├── Board/                — BoardCtrl
│   │   ├── Booster/              — 4 booster + Pos
│   │   ├── CellPlay/             — CellPlayCtrl
│   │   ├── Finding/              — FindingPath (BFS)
│   │   ├── GridSpot/             — GridSpotSpawn + animations
│   │   └── Tutorial/             — TutorialCtrl
│   ├── ObjPool/                  — BaseObjectPool, BlockItemSpawner, WallItemSpawner
│   ├── SO/                       — BoosterData, LevelData
│   ├── UI/                       — Popups, Screens
│   └── UserData/                 — UserData, SaveDataManager
├── SRCGame/ShinyEffectForUGUI/   — Hiệu ứng shine cho UI
├── StreamingAssets/, TextMesh Pro/
├── google-services.json          — Cấu hình Firebase (Android)
└── google-services-desktop.json  — Cấu hình Firebase (Editor)
```

---

## 3. Wireframe luồng tổng thể

```
                                       ┌──────────────────────┐
                                       │   APP LAUNCH         │
                                       │   (Init.unity)       │
                                       └──────────┬───────────┘
                                                  │
                                       ┌──────────▼───────────┐
                                       │  GameManager.Start() │
                                       │  - Set framerate     │
                                       │  - SaveDataManager.Load()
                                       │  - ChangeState(Loading)
                                       └──────────┬───────────┘
                                                  │
                                       ┌──────────▼───────────┐
                                       │  AddressableManager  │
                                       │  PreloadAllAssets    │
                                       │  ┌────────────────┐  │
                                       │  │ Wall/Container/│  │
                                       │  │ Block prefabs  │  │
                                       │  ├────────────────┤  │
                                       │  │ GridSpot       │  │
                                       │  ├────────────────┤  │
                                       │  │ LevelData SO   │  │
                                       │  │ (gom theo      │  │
                                       │  │  Level_X)      │  │
                                       │  └────────────────┘  │
                                       └──────────┬───────────┘
                                                  │ ShowLoadingEvent
                                                  │ Wait 2s
                                       ┌──────────▼───────────┐
                                       │  BackToMenu()        │
                                       │  → ChangeState(Menu) │
                                       └──────────┬───────────┘
                                                  │
                                       ┌──────────▼───────────┐
                                       │  Scene: UIMain.unity │
                                       │  ┌─────────────────┐ │
                                       │  │ ScreenMainMenu  │ │   ┌───────────────┐
                                       │  │   + PopupTab    │◀┼──▶│ PopupTab nav: │
                                       │  └─────────────────┘ │   │ Shop/Mission/ │
                                       │                      │   │ Menu/League/  │
                                       │                      │   │ Collection    │
                                       └──────┬───────────────┘   └───────────────┘
                                              │
                            ┌─────────────────┼────────────────────┐
                            │ [PLAY]          │ [⚙ Settings]       │ [❤ Heart]
                            ▼                 ▼                    ▼
                  ┌─────────────────┐  ┌──────────────────┐ ┌─────────────────┐
                  │ StartGame()     │  │ PopupSettings    │ │ PopupAddHeart   │
                  │ ChangeState(    │  │ UIMain (Google)  │ │ - Buy 500 coin  │
                  │   GamePlay)     │  └──────────────────┘ │ - Watch Ad      │
                  └────────┬────────┘                       │ - Friend request│
                           │                                └─────────────────┘
                ┌──────────▼──────────────┐
                │ PopupLoadingGamePlay    │
                │ Wait 2s                 │
                │ Load GamePlay.unity     │
                └──────────┬──────────────┘
                           │
                ┌──────────▼──────────────┐
                │  LevelManager.Init()    │
                │  ┌────────────────────┐ │
                │  │ LoadLevel(Round=0) │ │
                │  │ ├ BoardCtrl.Load   │ │      ┌─────────────────────┐
                │  │ ├ AddTutorial()    │◀┼─────▶│ Lvl 1 R0: Click     │
                │  │ └ IsBusy=false     │ │      │ Lvl 1 R1: Order     │
                │  └────────────────────┘ │      │ Lvl 2 R1: Pipe      │
                │  ShowScreen<GamePlay>   │      └─────────────────────┘
                └──────────┬──────────────┘
                           │
                ┌──────────▼──────────────────────────┐
                │  GAMEPLAY LOOP                      │
                │                                     │
                │  ┌───────────────────────────────┐  │
                │  │ Click block                   │  │
                │  │ → FindingPath.BFSFind         │  │
                │  │ → Animate path                │  │
                │  │ → CellPlay.CheckAndSave       │  │
                │  │ → CheckMatch_3 event          │  │
                │  └─────────┬─────────────────────┘  │
                │            │                         │
                │   ┌────────▼────────┐                │
                │   │ 3+ same type?   │                │
                │   └────┬────────┬───┘                │
                │     Yes│        │No                  │
                │   ┌────▼───┐    │                    │
                │   │ Merge  │    │                    │
                │   │ animate│    │                    │
                │   │ pool   │    │                    │
                │   └────┬───┘    │                    │
                │        │        │                    │
                │   ┌────▼────────▼────────────────┐  │
                │   │ checkWin / checkLose         │  │
                │   └────┬─────────────────────┬───┘  │
                │        │                     │      │
                │   Board hết │           Khay đầy 7  │
                │        ▼                     ▼      │
                │   Round++ < 3 ?         LoseGame()  │
                │   ├ Yes → LoadLevel(Round)         │
                │   └ No  → WinGame()                 │
                │                                     │
                └──────┬─────────────────────────┬───┘
                       │                         │
              ┌────────▼────────┐       ┌────────▼────────┐
              │  PopupWinGame   │       │  PopupLoseGame  │
              │  +100 coin fly  │       │  Trừ 1 tim      │
              │  Animation      │       │  Heartbeat anim │
              └────────┬────────┘       └────┬────────┬───┘
                       │                     │        │
                       │              [Try Again] [Menu]
                       │                     │        │
                       └──────────┬──────────┴────────┘
                                  │
                              BackToMenu()
                              → ChangeState(Menu)
```

---

## 4. Bootstrap & vòng đời ứng dụng

Trình tự khởi tạo game khi mở app:

| Bước | Thực thi bởi | Hành động |
|------|--------------|-----------|
| 1 | `Init.unity` | Scene đầu tiên load lên, chứa các DDOL singleton |
| 2 | `GameManager.Start()` | Set `Application.targetFrameRate = 60 (Android) / 120 (Editor)`; tắt VSync; gọi `SaveDataManager.Load()`; ChangeState(Loading) |
| 3 | `SaveDataManager.Load()` | Đọc `UserData.json` từ `Application.persistentDataPath`; nếu chưa có file → seed mặc định (5 tim, 2 mỗi loại booster) |
| 4 | `ChangeState(Loading)` | `UIManager.ShowPopup<PopupLoading>()` |
| 5 | `AddressableManager.Start()` | Tự động chạy `LoadAllAsset()` (async): preload prefabs label "Wall", "Container", "Block", "GridSpot" và toàn bộ `LevelData` label "Level" |
| 6 | Sau preload | Fire event `ShowLoading` → đợi 2s → `BackToMenu()` |
| 7 | `ChangeState(Menu)` | Load scene `UIMain.unity`; hiện `ScreenMainMenu` + `PopupTab` |

---

## 5. GameManager — máy trạng thái

**File:** `Assets/Scripts/GameManager/GameManager.cs`  
**Class:** `GameManager : SingletonDDOL<GameManager>`

### 5.1. Enum GameState

```
None → Loading → Menu → GamePlay → Win / Lose / Pause
```

### 5.2. Máy trạng thái

```
              ┌─────────┐
              │  None   │
              └────┬────┘
                   │
              ┌────▼────┐    Asset preload done
              │ Loading │────────────────────┐
              └────┬────┘                    │
                   │                         │
                   │   BackToMenu()          │
              ┌────▼────────────────────┐    │
              │       Menu              │◀───┘
              │ (UIMain.unity)          │
              │ ScreenMainMenu+PopupTab │
              └────┬────────────────────┘
                   │ StartGame()
              ┌────▼────────────────────┐
              │      GamePlay           │
              │ (GamePlay.unity)        │
              │ LevelManager + UI HUD   │
              └────┬─────────────┬──────┘
                   │             │
            WinGame()       LoseGame()
                   │             │
              ┌────▼───┐    ┌────▼─────┐
              │  Win   │    │  Lose    │
              │ Popup  │    │ Popup    │
              │Time=1  │    │Time=0    │
              └────┬───┘    └────┬─────┘
                   │             │
                   └─────┬───────┘
                         │ BackToMenu()
                         ▼
                       Menu
```

### 5.3. Hàm chính

| Hàm | Mục đích |
|-----|----------|
| `ChangeState(GameState)` (coroutine) | Điều phối mọi chuyển trạng thái: load scene, ẩn/hiện UI |
| `LoadSceneAndWait(string, Action)` | `SceneManager.LoadSceneAsync` rồi callback sau khi scene active |
| `StartGame()` | → ChangeState(GamePlay) |
| `WinGame()` | → ChangeState(Win) |
| `LoseGame()` | → ChangeState(Lose) |
| `BackToMenu()` | → ChangeState(Menu) |
| `PauseGame()` | Time.timeScale = 0, ChangeState(Pause) |
| `ResumeGame()` | Time.timeScale = 1, ChangeState(GamePlay) |

### 5.4. Field public

- `gameState : GameState`
- `Level : int` (lấy từ `UserData.level`)
- `OnGameStateChanged : Action<GameState>`

---

## 6. Hệ thống Singleton

**File:** `Assets/_Game/Common/Singleton.cs` (namespace `master`)

| Biến thể | Mục đích | Auto-Create | DontDestroyOnLoad |
|----------|----------|-------------|-------------------|
| `Singleton<T>` | Scope theo scene, dùng `FindObjectOfType` | Không | Không |
| `SingletonAutoCreate<T>` | Tự tạo nếu chưa có | Có | Không |
| `SingletonDDOL<T>` | Tồn tại xuyên scene | Không | **Có** |
| `SingletonDDOLAutoCreate<T>` | Tồn tại xuyên scene + tự tạo | Có | **Có** |
| `SingletonSO<T>` | Singleton dạng ScriptableObject | — | — |

### Bảng Manager đang dùng

| Manager | Biến thể | File | Ghi chú |
|---------|----------|------|---------|
| GameManager | DDOL | GameManager/GameManager.cs | Persist cross-scene |
| AddressableManager | DDOL | Addressables/AddressableManager.cs | Cache toàn bộ asset |
| CustomeEventSystem | DDOL | CustomeEvent/CustomeEventSystem.cs | Event bus toàn cục |
| AudioManager | Manual DDOL | _Game/Common/AudioManager.cs | Tự cài DontDestroyOnLoad |
| UserDataFirebaseManager | DDOL | Scenes/UserDataFirebaseManager.cs | Firebase API |
| HeartManager | SingletonAutoCreate | GameManager/HeartManager.cs | Tự tạo nếu thiếu |
| LevelManager | Singleton | LevelManager/LevelManager.cs | Scene `GamePlay.unity` |
| BoosterCtrl | Static Instance | LevelManager/Booster/BoosterCtrl.cs | Scene gameplay |
| CameraManager | Singleton | FitCamera/CameraManager.cs | Scene gameplay |
| UIManager | Singleton | _Game/Common/UIManager.cs | Mỗi scene UI |

---

## 7. Sơ đồ Scene flow

### 7.1. Init.unity (Bootstrap)

```
+---------------------- Init.unity ----------------------+
|  [GameManager]            (DDOL singleton)             |
|  [AddressableManager]     (DDOL, preload asset)        |
|  [AudioManager]           (DDOL, music + sfx)          |
|    ├─ MusicSource (AudioSource)                        |
|    └─ SoundSource (AudioSource)                        |
|  [CustomeEventSystem]     (DDOL, event bus)            |
|  [UserDataFirebaseManager](DDOL, Firebase)             |
|  [UIManager]                                           |
|    ├─ Canvas/Popup container                           |
|    └─ Canvas/Screen container                          |
|  [Directional Light] [Main Camera] [EventSystem]       |
+--------------------------------------------------------+
        │ ChangeState(Loading) → AddressableManager.LoadAllAsset()
        ▼
        Load UIMain.unity
```

### 7.2. UIMain.unity (Menu)

```
+--------------------- UIMain.unity ---------------------+
|  [Canvas + CanvasScaler]                               |
|  [EventSystem]                                         |
|  UIManager hiện:                                       |
|    • ScreenMainMenu (active)                           |
|    • PopupTab (overlay nav)                            |
|  Có thể mở thêm:                                       |
|    • PopupSettingsUIMain (Sign in Google)              |
|    • PopupAddHeart                                     |
|    • ScreenShop / Mission / League / Collection (tab)  |
+--------------------------------------------------------+
```

### 7.3. GamePlay.unity

```
+--------------------- GamePlay.unity --------------------+
|  [Directional Light] [Main Camera] [Global Volume]      |
|  [Canvas] — chứa ScreenGamePlay overlay                 |
|                                                         |
|  [LevelManager] (root, Singleton)                       |
|    ├─ BoardCtrl                                         |
|    ├─ CellPlayController (CellPlayCtrl)                 |
|    ├─ BoosterCtrl                                       |
|    │   ├─ BoosterUndo                                   |
|    │   ├─ BoosterAdd + BoosterAddPos                    |
|    │   ├─ BoosterShuffle + BoosterShufflePos            |
|    │   └─ BoosterMagnet + BoosterMagnetPos              |
|    └─ TutorialCtrl                                      |
|                                                         |
|  [Tutorial]      — root popup pos                       |
|  [Pos_0 … Pos_34]— ô lưới chuẩn (5×7)                  |
|  [Prefabs]       — runtime container cho block          |
+---------------------------------------------------------+
```

### 7.4. Test.unity / TestFireBase.unity

Sandbox riêng — không nằm trong luồng chính.

---

## 8. AddressableManager

**File:** `Assets/Scripts/Addressables/AddressableManager.cs`  
**Class:** `AddressableManager : SingletonDDOL<AddressableManager>`

### 8.1. Cache nội bộ

```csharp
Dictionary<string, GameObject>      _cachedPrefabs;
Dictionary<string, List<LevelData>> _cachedLevelGroups;
bool                                _isPreloaded;
```

### 8.2. Luồng preload

```
Start()
  └─ LoadAllAsset() (async)
       ├─ PreloadAllPrefabsAsync()  → labels: "Wall", "Container", "Block"
       ├─ PreloadAllGridSpotsAsync()→ label: "GridSpot"
       ├─ PreloadAllLevelsAsync()   → label: "Level"
       ├─ _isPreloaded = true
       ├─ Wait 2s
       ├─ CustomeEventSystem.ShowLoading()
       ├─ Wait 2s
       └─ GameManager.BackToMenu()
```

### 8.3. Quy ước tên Level

- Tên file: `Level_X(board_N).asset` (X = level, N = round 1..3)
- `GetBaseLevelName(asset.name)` → tách `Level_X`
- `ExtractBoardIndex(asset.name)` → tách N rồi sort group theo N
- `GetLevelGroup("Level_" + GameManager.Level)` → trả về `List<LevelData>` đã sort

### 8.4. API public

| Hàm | Trả về | Mục đích |
|-----|--------|----------|
| `GetLevelGroup(string)` | `List<LevelData>` | Lấy tất cả round của 1 level |
| `GetAllLevelNames()` | `List<string>` | Tên các level đã load |
| `GetPrefab(string)` | `GameObject` | Lấy prefab cache theo tên |
| `LoadPrefabAsync(string)` | `Task<GameObject>` | Load tức thì nếu chưa có |
| `InstantiatePrefabAsync(...)` | `Task<GameObject>` | Instantiate có async |
| `ReleaseAll()` | void | Giải phóng cache |

**⚠ Quan trọng:** mọi prefab được dùng trong `LevelData.prefabNames[]` BẮT BUỘC phải có Addressable name trùng — nếu không, lookup ở runtime sẽ trả về null.

---

## 9. CustomeEventSystem — event bus

**File:** `Assets/Scripts/CustomeEvent/CustomeEventSystem.cs`  
**Class:** `CustomeEventSystem : SingletonDDOL<CustomeEventSystem>`

### Danh sách event đầy đủ

| Event Action | Tham số | Trigger | Mục đích |
|--------------|---------|---------|----------|
| `ChangeRoundAction` | `int Round` | `LevelManager.NextRoundLevel()` | Cập nhật chỉ báo Round trên UI |
| `ResetStartAction` | — | (reserved) | Reset round |
| `ChangeCoinAction` | `int Coin` | Khi cộng/trừ coin | Refresh số coin UI |
| `ChangeLevelAction` | `int Level` | Khi pass level | Refresh số level UI |
| `ShowLoadingAction` | — | `AddressableManager` sau preload | Hiện loading hoàn tất |
| `CheckMatch_3_Action` | `TypeItem` | `ItemClickCtrl.Checkmatch_3` | Trigger merge 3 |
| `ActiveBoosterAction` | `List<int>` | `LevelManager`, booster | Bật/tắt nút booster ([Undo, Add, Shuffle, Magnet]) |
| `TutorialPosAction` | `TutorialMode, Vector3` | `TutorialCtrl.TutorialClick()` | Đặt vị trí hand pointer |
| `ShowTextMatch_3_Action` | `bool` | Tutorial step | Ẩn/hiện text "Match 3" |
| `ChangeTextTutorialAction` | `TutorialType` | Tutorial step | Đổi text tutorial |

**Pattern subscribe:** `OnEnable() += …`, `OnDisable() -= …`.

---

## 10. AudioManager

**File:** `Assets/_Game/Common/AudioManager.cs`

- Manual singleton (gán `Instance` trong `Awake()`, gọi `DontDestroyOnLoad`).
- 1 `musicSource` + 1 `soundSource` + 5 source clone → pool 6 SFX.
- Lưu setting qua `PlayerPrefs`: `AudioMusicSetting`, `AudioSoundSetting`, `AudioVibrateSetting`, `Ratio_Sound`.
- `Ratio_Sound` mặc định: **0.5 trên Desktop**, **1.0 trên Android**.

### Hàm chính

| Hàm | Mục đích |
|-----|----------|
| `PlayOneShot(string name, float vol, float delay)` | Phát SFX theo tên (pool) |
| `Play(string name, float vol, bool loop)` | Phát nhạc nền theo tên |
| `StopMusic()` / `StopSFX()` | Dừng |
| `PauseMusic()` / `ResumeMusic()` | Tạm dừng |
| `SetCacheAudio()` / `ResetAudio()` | Lưu state nhạc khi app pause/resume |
| `FixVolumeMusic()` / `FixVolumeSFX()` | Áp volume qua AudioMixer (dB = 20*log10) |
| `PlayVibrate()` | Rung haptic nếu bật |

### Pool logic

- `GetAudioSource()` lấy source rảnh; nếu hết → tái dùng source đầu.
- Throttle: nếu đã có > 10 SFX đang phát thì bỏ qua.

---

## 11. LevelManager & các sub-controller

**File:** `Assets/Scripts/LevelManager/LevelManager.cs`  
**Class:** `LevelManager : Singleton<LevelManager>`

### 11.1. Kiến trúc tổ hợp

```
LevelManager (orchestrator)
   │
   ├─ BoardCtrl       ─ Quản lý lưới bàn chơi
   ├─ CellPlayCtrl    ─ Quản lý khay 7 ô + match-3
   ├─ BoosterCtrl     ─ Cổng vào 4 booster, cờ IsBusy
   └─ TutorialCtrl    ─ Trỏ tay + text hướng dẫn
```

### 11.2. Field public

| Field | Kiểu | Mô tả |
|-------|------|-------|
| `BoardCtrl` | BoardCtrl | Bàn chơi |
| `cellPlayCtrl` | CellPlayCtrl | Khay nhận |
| `boosterCtrl` | BoosterCtrl | Booster controller |
| `TutorialCtrl` | TutorialCtrl | Tutorial controller |
| `levelDatas` | List<LevelData> | 3 board cho level hiện tại |
| `Round` | int | Round hiện tại (0,1,2) |
| `NextRound` | Func<IEnumerator> | Delegate khi cần next round |
| `isNextRound` | bool | Cờ chặn double-trigger |

### 11.3. Hàm chính

#### `Init()`
1. Start coroutine `LoadLevel()`.
2. Subscribe `NextRoundLevel` vào `NextRound`.

#### `LoadLevel()` (coroutine)
1. `Round = 0`.
2. `levelDatas = AddressableManager.GetLevelGroup("Level_" + GameManager.Level)`.
3. `BoosterCtrl.IsBusy = true` — khóa booster.
4. `BoardCtrl.LoadLevel(levelDatas[0], false)`.
5. `AddTutorial()`.
6. Wait 0.3s.
7. `BoosterCtrl.IsBusy = false`.

#### `AddTutorial()` (hard-code)
```
Level == 1:
   Round == 0  → PopupTutorial + ShowText(Click) + TutorialClick()
   Round >= 1  → ShowText(Order) + Hide Match_3 text
Level == 2:
   Round == 1  → ShowText(Pipe) + Hide Match_3 text
```

#### `NextRoundLevel()` (coroutine — chuyển round)
1. Guard `isNextRound`.
2. Wait 1.5s, phát SFX `BLJ_League_LeaderBoard_Enter`.
3. `AddPool()` — trả pool về.
4. `Round++`. Nếu `Round > 2` → kết thúc level (Win flow xử lý ở `checkWin`).
5. Fire `ChangeRound(Round)` + `ActiveBooster({-1,-1,1,1})` (bật Shuffle + Magnet).
6. `BoosterCtrl.IsBusy = true`, reset Undo stack.
7. `BoardCtrl.LoadLevel(levelDatas[Round])`.
8. `AddTutorial()`, wait 0.3s, `IsBusy = false`.

#### `AddPool()`
- `BlockItemSpawner.Instance.AddBlockInPool()` — trả block đã match về pool.
- `WallItemSpawner.Instance.AddOtherInPool()` — trả wall/container về pool.

---

## 12. BoardCtrl — bàn chơi

**File:** `Assets/Scripts/LevelManager/Board/BoardCtrl.cs`

### 12.1. Cấu trúc dữ liệu lưới

| Field | Kiểu | Ý nghĩa |
|-------|------|---------|
| `grid[,]` | BoardCell | Mảng 2 chiều block trên bàn |
| `IsWall[,]` | bool | Đánh dấu ô tường |
| `gridContainerSpot[,]` | Container | Container song song với grid |
| `boardCells` | List<BoardCell> | Tất cả block đang sống |
| `boardAlls` | List<GameObject> | Toàn bộ object theo thứ tự row-major |
| `gridSpotSpawns` | List<GridSpotSpawn> | Tất cả máng sinh |
| `itemClickCtrl` | ItemClickCtrl | Xử lý click |

### 12.2. Hàm public

| Hàm | Mục đích |
|-----|----------|
| `LoadLevel(LevelData, bool isSlide=true)` | Build bàn từ LevelData |
| `SpawnLeaderBoard(grid, IsWall, container)` | Đọc `prefabNames[y*w+x]`, instantiate block/wall/container/GSP |
| `AddNeighbor(grid, IsWall)` | Gán 4 hàng xóm + kích hoạt block có cạnh hở |
| `AlignContainer(container)` | Liên kết GridSpotSpawn với Container kề bên |
| `CheckSpawnBlock(Container, BoardCell)` | Sinh block kế tiếp khi container trống |
| `RebuildGridFromBoardAlls()` | Dựng lại `grid[,]` từ `boardAlls` (sau Shuffle) |
| `GetNextRandomType(int totalUnits)` | Chọn type ngẫu nhiên có trọng số (mỗi type ≥ 3) |
| `UpdateBoardCell(BoardCell)` | Xóa khỏi danh sách (khi block biến mất) |
| `AddBlockInLeaderBoard(Container, BoardCell)` | Thay container bằng block sau Undo |
| `DeleteLeaderBoardOld()` | Tắt object cũ giữa các round |
| `FolowCamera()` | Điều chỉnh camera theo `levelData.alignment` |
| `SlideLeaderBoard(bool)` | Animation slide-in bàn (DOTween 0.25s) |

### 12.3. Quy ước `prefabNames`

Mỗi ô đánh tên dạng string:

- `null`/`""` — rỗng
- `"1"`..`"7"` — block 7 loại (BlueBase, BrownBase, GreenBase, MagentaBase, RedBase, PurpleBase, YellowBase)
- `"1B"`..`"7B"` — block bị **barrel** chặn
- `"Wall"` — tường
- `"Container"` — chỉ container (không có block trên)
- `"GSPDown"`, `"GSPUp"`, `"GSPLeft"`, `"GSPRight"`, `"GSPBottomRight"` — máng sinh

---

## 13. CellPlayCtrl — khay nhận block

**File:** `Assets/Scripts/LevelManager/CellPlay/CellPlayCtrl.cs`

### 13.1. Cấu trúc dữ liệu

| Field | Kiểu | Ý nghĩa |
|-------|------|---------|
| `boardCells` | List<BoardCell> | Block đang ở khay (max 7) |
| `cellPlays` | List<Container> | 7 ô khay cố định |
| `countCellType` | Dictionary<TypeItem, List<BoardCell>> | Đếm theo loại |
| `boardCellMatch_3` | List<BoardCell> | Block đã match đang chờ pool |
| `orderPlayInCellPlay` | List<TypeItem> | Thứ tự loại vào khay |
| `posCellPlays` | Queue<int> | Index ô vừa được lấp |

### 13.2. Luồng nhận & merge

```
Click block ─▶ ItemClickCtrl.OnClickItem
              ├─ FindingPath.BFSFind(container)
              ├─ BoardCellMovement.MovementPath(path)
              └─ CellPlayCtrl.CheckAndSaveBoardCell(cell)
                  ├─ FindInsertIndex(cell.TypeItem)  ← gom cùng loại
                  ├─ ShiftCellsRight (nếu cần)       ← animate đẩy phải
                  ├─ Set IsInCellPlay = true
                  └─ countCellType[type].Add(cell)
                       │
                       ▼
                  Checkmatch_3(boardCell)
                  ├─ HasMatch3() check
                  └─ CustomeEventSystem.CheckMatch_3(type)
                       │
                       ▼
                  CellPlayCtrl.Match_3(type) → Match3Process()
                  ├─ Đợi 3 cell settle (dist < 0.01)
                  ├─ RemoveCellData(c1, c2, c3)
                  ├─ SetAnimMerge(c1, c2, c3)
                  │    ├─ Khóa Undo tạm thời
                  │    ├─ Wait rotation
                  │    ├─ Raise animation
                  │    ├─ Merge to center (DOMove + scale 1.2x)
                  │    ├─ Pop animation
                  │    └─ Deactivate → boardCellMatch_3
                  ├─ RearrangeCellsAfterRemove() (DOTween.Sequence parallel)
                  └─ checkWin() / checkLose()
```

### 13.3. Điều kiện Win/Lose

- **Win:** `BoardCtrl.BoardCells.Count == 0 && Round >= 2` → `GameManager.WinGame()`
- **Lose:** `boardCells.Count == 7 && tất cả IsInCellPlay == true` mà không match được → `GameManager.LoseGame()`

---

## 14. BoosterCtrl & 4 Booster

**File:** `Assets/Scripts/LevelManager/Booster/BoosterCtrl.cs`

```csharp
public class BoosterCtrl : MonoBehaviour {
    public static BoosterCtrl Instance;
    public bool IsBusy;                  // Khóa input khi true
    public BoosterAdd     BoosterAdd;
    public BoosterMagnet  BoosterMagnet;
    public BoosterShuffle BoosterShuffle;
    public BoosterUndo    BoosterUndo;
}
```

`IsBusy = true` chặn:
- Click block (`ItemClickCtrl.OnClickItem` early return).
- Click nút booster UI.
- Đang load round / đang thực thi booster.

### 14.1. Booster UNDO

**File:** `BoosterUndo/BoosterUndo.cs`

```csharp
Stack<(BoardCell, Container, List<Vector3>)> LastMove;
Stack<Queue<KeyValuePair<BoardCell, Container>>> UndoQueue; // cho Match_3
Stack<bool> IsMatch3s;
```

- Mỗi click block → `AddStack` lưu (block, container, path).
- Khi Match_3 → đẩy thêm 2 block cùng loại vào queue.
- `Undo()` → pop `IsMatch3s`:
  - `false` → `UndoNormalMove()` đảo path trở lại.
  - `true` → `UndoMatch3Move()` tái tạo block đã merge.
- Sau Undo: stack reset; nút Undo chỉ hiện khi `IsMatch3s.Count > 0`.

### 14.2. Booster ADD

**File:** `BoosterAdd/BoosterAdd.cs` + `BoosterAddPos.cs`

- Lấy 3 block đầu tiên trên khay → chuyển xuống "Add Zone" (3 vị trí cố định bên dưới khay).
- Block ở Add Zone không thể match-3, có cờ `IsBoosterAdd = true`.
- Khi click block trong Add Zone → chuyển lại khay (ô trống tiếp theo).

### 14.3. Booster SHUFFLE

**File:** `BoosterShuffle/BoosterShuffle.cs`

- Duyệt `boardAlls`, lọc các block có tên là số (1-7) — bỏ qua Wall/Container/GSP/Barrel.
- Fisher-Yates shuffle các index → swap từng cặp (DOMove 0.5s).
- Gọi `BoardCtrl.RebuildGridFromBoardAlls()`.
- Re-evaluate active neighbor (block trước đó bị chặn có thể bị mở khóa).

### 14.4. Booster MAGNET

**File:** `BoosterMagnet/BoosterMagnet.cs` + `BoosterMagnetPos.cs`

- Quét khay → chọn loại có nhiều block nhất, thứ tự vào khay sớm nhất.
- Tính `maxMagnet = 3 - currentTrayCount`.
- Thu thập từ bàn chơi (cùng loại, chưa ở khay); nếu thiếu → sinh thêm từ `GridSpotSpawn.SpawnBlockMagnet()`.
- Phase 1 **Knob**: block xoay/wobble 0.35s.
- Phase 2 **Move**: trượt về Magnet Zone.
- Phase 3 **Merge**: gọi `SetAnimMerge()` → Match_3.

---

## 15. FindingPath — BFS

**File:** `Assets/Scripts/LevelManager/Finding/FindingPath.cs`

Thuật toán Breadth-First Search để tìm đường từ block được click → hàng cuối (khay):

```
BFSFind(Container start)
├─ Tính (startRow, startCol)
├─ Nếu đã ở hàng cuối → return [], true
└─ FindPath:
    ├─ Queue + visited[,] + parent[,]
    ├─ Lan tỏa 4 hướng:
    │   • valid (trong biên)
    │   • container tồn tại
    │   • !IsContaining (không bị chặn)
    │   • !IsWall (qua getContainer)
    ├─ Khi chạm hàng cuối → ReconstructPath()
    └─ Trả về List<Vector3> theo thứ tự start → end
```

Đường đi này được `BoardCellMovement.MovementPath()` dùng để animate block trượt.

---

## 16. TutorialCtrl

**File:** `Assets/Scripts/LevelManager/Tutorial/TutorialCtrl.cs`

### Enum `TutorialType`
`Click` (Lvl 1 R0), `Order` (Lvl 1 R1+), `Pipe` (Lvl 2 R1), `Match_3` (chưa dùng trong lvl 1-2).

### Hàm
| Hàm | Mục đích |
|-----|----------|
| `BlockNeedClick()` | Quét `boardAlls` ngược, trả về vị trí block đầu tiên click được |
| `TutorialClick()` | Phát `TutorialPosAction(TutorialMode.GamePlay, pos)` |
| `ShowOrHideTextMatch_3(bool)` | Bật/tắt text Match-3 |
| `ShowText(TutorialType)` | Đổi text tutorial qua `ChangeTextTutorialAction` |

---

## 17. GridSpot — máng sinh block

**File:** `Assets/Scripts/LevelManager/GridSpot/Spawn/GridSpotSpawn.cs`

### Field

```csharp
int currentPointSpawn;   // số lần sinh còn lại
int maxPointSpawn;       // max ban đầu
Direction[] directions;  // hướng (Up/Down/Left/Right)
Dictionary<Direction, Container> containers;
Stack<BoardCell> JustSpawns;            // dùng cho Undo
BaseGridSpotAnimation baseGridSpotAnimation;
```

### Hàm

| Hàm | Mục đích |
|-----|----------|
| `SpawnBlock(prefab, container, type, onSpawned)` | Sinh block từ máng → container; animate exit + scale + di chuyển |
| `SpawnBlockMagnet(...)` | Sinh cho booster Magnet (không gán container) |
| `CheckDirection(Direction)` | Có hỗ trợ hướng này không |
| `AddContainer(Container, Direction)` | Map container vào hướng |
| `DestroyBoardCellJustSpawn()` | Hủy block vừa sinh nếu Undo |

Animation theo hướng có lớp con: `GSPDownAnimationCtrl`, `GSPLeftAnimationCtrl`, `GSPRightAnimationCtrl`, `GSPBottomRightAnimationCtrl`.

---

## 18. Object Pool

**File:** `Assets/Scripts/ObjPool/BaseObjectPool.cs`, `BlockItemSpawner.cs`, `WallItemSpawner.cs`

```
BaseObjectPool
├─ List<Transform> prefabs        — Master prefab (con của "Prefabs" holder)
├─ List<Transform> poolObjs       — Đã despawn, đợi tái dùng
├─ Spawn(name, pos, rot)
├─ Despawn(transform)
├─ getObjectFromPool(prefab)
└─ LoadPrefabs() / HidePrefabs()

BlockItemSpawner : BaseObjectPool
├─ spawnCellItem(typeName, pos, rot)
├─ Despawn(transform)             — Cộng vào boardCellPools
└─ AddBlockInPool()               — Reinit + despawn tất cả block đã match

WallItemSpawner : BaseObjectPool
├─ SpawnOtherItem(name, pos, rot)
└─ AddOtherInPool()               — Trả wall/container về pool
```

Mục đích: tránh `Instantiate/Destroy` gây GC spike khi đổi round.

---

## 19. LevelData & Level Editor

### 19.1. LevelData (ScriptableObject)

**File:** `Assets/Scripts/SO/LevelData/LevelData.cs`

```csharp
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data (Prefab Mode)")]
public class LevelData : ScriptableObject {
    public int      width;
    public int      height;
    public string[] prefabNames;   // index = y * width + x  (row-major)
    public bool     alignment;     // offset camera
    public int      totalUnits;    // tổng số block (cân bằng spawn random)
    public List<GSPData> gsp;      // máng sinh
}

[Serializable]
public class GSPData {
    public int    x, y;
    public int    spawnCount;
    public string type;        // "GSPDown", "GSPLeft", "GSPBottomRight", …
}
```

### 19.2. Hàm tiện ích

| Hàm | Mục đích |
|-----|----------|
| `GetPrefabName(x, y)` | Lookup `prefabNames[y*width+x]` |
| `GetGSPAt(x, y, type)` | Tìm máng theo vị trí + type |
| `CopyFrom(LevelData)` | Copy field |
| `ShufflePrefabs()` | Ngẫu nhiên hoá vị trí block, **GIỮ NGUYÊN** mọi prefab có tên chứa `wall`, `container`, `gsp`, `b` (barrel) |

### 19.3. Tổng số level

- 10 thư mục `Level_1` … `Level_10`, mỗi level 3 board → **30 asset** tổng.

### 19.4. Level Editor

**File:** `Assets/Scripts/Editor/LevelEditorWindow.cs` (menu `Game Tools → Level Editor`)

- Cho phép set width/height (resize `prefabNames`).
- Toolbar tile palette: Empty, Wall, Container, `1..7`, `1B..7B`, các GSP*.
- Click ô lưới → đặt tile theo palette.
- Panel cài `spawnCount` cho từng GSP.
- Save → tạo asset `LevelData` vào folder chỉ định.

---

## 20. UI Framework — UIManager

**File:** `Assets/_Game/Common/UIManager.cs`  
**Class:** `UIManager : Singleton<UIManager>`

### 20.1. Khái niệm

- **ScreenUI**: panel full-screen, chỉ 1 active tại 1 thời điểm.
- **PopupUI**: overlay, có thể chồng nhiều cái.

### 20.2. API public

| Hàm | Mục đích |
|-----|----------|
| `ShowScreen<T>()` | Hiện screen T; tự deactive screen cũ |
| `GetScreen<T>()` / `GetScreenActive<T>()` | Lấy reference |
| `ShowPopup<T>(onClose)` | Hiện popup T (animation MoveMent / ScalePunch) |
| `GetPopup<T>()` / `GetPopupActive<T>()` | Lấy reference |
| `HideAllPopup()` | Ẩn (không destroy) tất cả popup |
| `HasPopupShowing()` | Có popup nào đang hiện không |
| `NotifyContent(str, color, duration)` | Toast nổi |

### 20.3. Tải prefab

```
Screens: Resources.Load<T>("UI/Screens/" + typeName)
Popups:  Resources.Load<T>("UI/Popups/"  + typeName)
```

### 20.4. Base class

```
ScreenUI                       PopupUI
├─ isCache (giữ lại?)          ├─ isCache
├─ Active()                    ├─ isShowing
├─ Deactive()                  ├─ Show(onClose)
└─ OnDestroyScreen event       ├─ Hide()
                               ├─ enum AnimShowPopUp { None, MoveMent, ScalePunch }
                               └─ OnShow / OnHide events
```

---

## 21. Screens — wireframe từng màn hình

### 21.1. ScreenMainMenu

**File:** `Assets/Scripts/UI/Screens/ScreenMainMenu.cs`

Hub menu chính, tích hợp hệ thống tim.

```
+----------------------------------------------+
|  [⚙ Setting]              Coin: 9999  💰     |
|                                              |
|                                              |
|              🎮  PLAY                        |
|             Level 10                         |
|                                              |
|                                              |
|         ❤️ ❤️ ❤️ ❤️ ❤️    5/5                  |
|             2m 30s  (timer or FULL)          |
|         (chạm ❤ → PopupAddHeart)             |
+----------------------------------------------+
```

**Event:** `HeartManager.OnHeartsChanged` → `RefreshHeartUI()`.

### 21.2. ScreenGamePlay

**File:** `Assets/Scripts/UI/Screens/ScreenGamePlay.cs`

HUD trong gameplay với 4 booster + chỉ báo round.

```
+----------------------------------------------+
|  [⚙ Setting]                Coin: 2050  💰   |
|                                              |
|       Level 5                                |
|                                              |
|       ●  ●  ●     (Round_1, Round_2, Round_3)|
|       ▲  ▲  ▲     IconRoundDo = đã pass      |
|                                              |
|       ============= BÀN CHƠI (3D) ==========  |
|              (rendered ngoài Canvas)         |
|       =====================================  |
|                                              |
|       ============= KHAY 7 Ô ===============  |
|              (rendered ngoài Canvas)         |
|       =====================================  |
|                                              |
|  ┌─────┐  ┌─────┐   ┌─────┐  ┌─────┐         |
|  │⏮Undo│  │🛠Add │   │🔀Shuf│  │🧲Mag │         |
|  │  x2 │  │ $100 │  │ $50 │  │ $75 │         |
|  └─────┘  └─────┘  └─────┘  └─────┘         |
+----------------------------------------------+
```

- 4 nút booster đọc `BoosterData.price` & `UserData.listBoosterCounters`.
- Nếu count > 0 → hiển thị "x2", click trừ counter.
- Nếu count = 0 → hiển thị `price`, click trừ coin.
- Animation: loop Yoyo InOutSine cho icon; click PunchScale + Rotate.

**Subscribe:** `ChangeRoundAction`, `ChangeCoinAction`, `ActiveBoosterAction`.

### 21.3. ScreenShop

```
+----------------------------------------------+
|  SHOP                          Coin: 5000    |
|                                              |
|   ┌─────────────────────────────────────┐    |
|   │  Undo Pack x5            $4.99      │    |
|   ├─────────────────────────────────────┤    |
|   │  Add Pack x5             $4.99      │    |
|   ├─────────────────────────────────────┤    |
|   │  Shuffle Pack x5         $3.99      │    |
|   ├─────────────────────────────────────┤    |
|   │  Hearts x3               $2.99      │    |
|   └─────────────────────────────────────┘    |
+----------------------------------------------+
```

> Hiện tại Shop chỉ có 1 TMP hiển thị coin (`UpdateCoinText()`); IAP chưa cài.

### 21.4. ScreenMission (stub)

```
+----------------------------------------------+
|  MISSIONS                                    |
|                                              |
|   • Daily Challenge #1       [_]             |
|     Clear 50 blocks         +50 coin         |
|                                              |
|   • Daily Challenge #2       [_]             |
|     Match 5 same colors     +100 coin        |
+----------------------------------------------+
```

### 21.5. ScreenLeague (stub — wrapper cho leaderboard)

```
+----------------------------------------------+
|  LEAGUE                  Global Top 100      |
|                                              |
|   1️⃣  Player1               Level 150       |
|   2️⃣  Player2               Level 148       |
|   3️⃣  Player3               Level 145       |
|   ...                                        |
|   🏅 YOU                    Level 42         |
+----------------------------------------------+
```

### 21.6. ScreenCollection (stub)

```
+----------------------------------------------+
|  COLLECTION                                  |
|                                              |
|   🎨 Avatar Default     UNLOCKED             |
|   🎨 Avatar Gold        UNLOCKED             |
|   🎨 Avatar Cyber       LOCKED               |
|                                              |
|   🏆 Badge Master       UNLOCKED             |
|   🏆 Badge Legend       LOCKED               |
+----------------------------------------------+
```

### 21.7. ScreenRanking (Leaderboard cụ thể)

Thư mục: `Assets/Scripts/UI/Screens/ScreenRanking/`

```
+----------------------------------------------+
|     [ FRIENDS ]   [ PLAYERS ]                |
| ----- TabContent -----                       |
|                                              |
|  Tab Friends:                                |
|   • #1 friend1     Lv 50                     |
|   • #2 friend2     Lv 30                     |
|   • #3 YOU         Lv 10                     |
|                                              |
|   [ + Add Friend ]   [ Friend Requests ]     |
|                                              |
|  Tab Players (global):                       |
|   • #1 globalA     Lv 200                    |
|   • #2 globalB     Lv 150                    |
|   ...                                        |
|                                              |
|   [📋 Copy My ID]                            |
+----------------------------------------------+
```

---

## 22. Popups — wireframe từng popup

### 22.1. PopupLoading

```
+----------------------------------------------+
|                                              |
|                                              |
|              ⟳  LOADING  ⟲                    |
|              (spinning icon)                 |
|                                              |
+----------------------------------------------+
```

### 22.2. PopupLoadingGamePlay

```
+----------------------------------------------+
|                                              |
|     [Random Background Image]                |
|                                              |
|              L o a d i n g                   |
|              ~~~~~~~~~~~~~ (wave anim)       |
|                                              |
+----------------------------------------------+
```

**Hiệu ứng:** TMP vertex wave (amplitude=5, frequency=2, speed=5) — mỗi ký tự lên xuống theo `Mathf.Sin()`.

### 22.3. PopupTab (luôn hiện ở Menu)

```
+----------------------------------------------+
|  [Shop] [Mission] [Menu↑] [League] [Coll]    |
|         (selected button phóng 1.2x)         |
|                                              |
|   === Choice Panel (selected sits here) ===  |
|                                              |
|   [   SCREEN CONTENT BÊN DƯỚI THEO TAB   ]   |
|                                              |
+----------------------------------------------+
```

**Enum `StatusChoice`:** 0=Shop, 1=Mission, 2=MainMenu, 3=League, 4=Collection.  
Khi đổi tab: scale nút cũ về 1, di chuyển nút mới vào ChoicePanel, scale lên 1.2 (0.1s).

### 22.4. PopupSettings (trong gameplay)

```
+----------------------------------------------+
|  ⚙  GAME SETTINGS                            |
|                                              |
|   [🔊 SOUND]      ON   ← toggle              |
|   [📳 VIBRATION]  ON   ← toggle              |
|                                              |
|   [    QUIT LEVEL   ]  (trừ 1 tim, về Menu)  |
|                                              |
|   [    CLOSE        ]                        |
+----------------------------------------------+
```

Nút sound/vibration đổi sprite + tint (`Color.white` ↔ `0.45 grey`).

### 22.5. PopupSettingsUIMain (Menu)

```
+----------------------------------------------+
|  ⚙  SETTINGS                                 |
|                                              |
|   [ 🔑 SIGN IN WITH GOOGLE ]                 |
|   (Sync progress to cloud)                   |
|                                              |
|   [ CLOSE ]                                  |
+----------------------------------------------+
```

Gọi `UserDataFirebaseManager.LinkGoogleAccount()` (yêu cầu package `com.unity.services.authentication`).

### 22.6. PopupTutorial

```
+----------------------------------------------+
|                                              |
|     ✋  (Hand icon trỏ vào block, scale yoyo) |
|                                              |
|     "Only Blockies with an open path         |
|      can be tapped"                          |
|       ~~~~~~~~ (wave vertex anim)            |
|                                              |
|     ┌───────────────────────────────┐        |
|     │   MATCH 3 COLORS              │        |
|     │   ~~~~~~~~ (brown ↔ green)    │        |
|     └───────────────────────────────┘        |
+----------------------------------------------+
```

**Subscribe:**  
- `TutorialPosAction` → đặt vị trí hand.  
- `ShowTextMatch_3_Action` → bật/tắt khung Match-3.  
- `ChangeTextTutorialAction` → đổi text theo `TutorialType`.

### 22.7. PopupWinGame

```
+----------------------------------------------+
|                                              |
|           🎉  LEVEL 9 PASSED!  🎉            |
|                                              |
|        💰  💰  💰  ↓                          |
|        ↓   ↘   ↙   (20 coin fly from         |
|        ↓   ↘   ↙    source → destination)   |
|                                              |
|        TOTAL: 9 9 9 9   (tween OutCubic)     |
|                                              |
|        [ TAP TO CONTINUE ]                   |
+----------------------------------------------+
```

**Animation:**
1. Delay 2s → SFX `BLJ_Game_Obstacles_DestructibleWall_Finish`.
2. Spawn 20 coin tại `source`, scale 0.25x, fly arc (OutQuad 0.6s + InQuad 0.4s), random offset.
3. Tween số coin currentCoin → targetCoin trong 0.5s.
4. Hiện nút `TAP TO CONTINUE` với PunchScale.

`OnOkClicked()` → `SaveDataManager.Save()` → `GameManager.BackToMenu()`.

### 22.8. PopupLoseGame

```
+----------------------------------------------+
|                                              |
|           ❌  LEVEL FAILED  ❌               |
|                                              |
|              💔 💗 💔                          |
|         (Heartbeat scale 1↔1.2 yoyo,         |
|          chạy dù Time.timeScale=0)           |
|                                              |
|         ❤️ HEARTS: 2 / 5                      |
|                                              |
|   [ TRY AGAIN ]        [ MENU ]              |
+----------------------------------------------+
```

`Show()` consume 1 tim.  
`Try Again`: nếu hết tim → mở `PopupAddHeart`, ngược lại → `StartGame()`.

### 22.9. PopupAddHeart

```
+----------------------------------------------+
|     ❤️  ADD HEARTS                           |
|                                              |
|     Hearts: 2 / 5   FULL                     |
|     Next heart in 3m 45s                     |
|                                              |
|     [ 💰 BUY (500 coins) ]                   |
|     [ 📺 WATCH AD ]                          |
|     [ 👫 REQUEST FROM FRIEND ]               |
|                                              |
|     [ CLOSE ]                                |
+----------------------------------------------+
```

**Hằng số:** `COIN_COST_PER_HEART = 500`.

- **Buy:** trừ 500 coin, `HeartManager.Add(1)`, `SaveDataManager.Save()`.
- **Watch Ad:** chưa tích hợp ad SDK; Editor → cộng 1 tim ngay.
- **Request from Friend:** ẩn popup, mở `ScreenLeague` để gửi xin tim.

> 📌 Lưu ý: memory có ghi quy ước "buy-1-heart cost = win-coin-reward × 10". Trong code hiện tại giá đang **hard-code 500**. Phần thưởng win là 100 coin (`PopupWinGame`), suy ra quy ước → 1000. **Có sự lệch giữa quy ước và code** — cần xác nhận lại.

### 22.10. PopupSendGilf (gửi quà)

```
+----------------------------------------------+
|  SEND GIFT TO FRIEND                         |
|                                              |
|   [⏮ Undo]    [✓]                            |
|   [🛠 Add]                                   |
|   [🔀 Shuffle]                               |
|   [❤️ Heart]                                  |
|                                              |
|   [ SEND ]                                   |
|                                              |
|   "You've sent 2/3 gifts today"              |
+----------------------------------------------+
```

Map: btnFreeze→Undo, btnHammer→Add, btnBomb→Shuffle, btnHeart→Heart.  
Send → `UserDataFirebaseManager.SendBooster(from, to, name, amount)` (giới hạn 3 lần/ngày, timezone UTC+7).

---

## 23. Hệ thống Tim (HeartManager)

**File:** `Assets/Scripts/GameManager/HeartManager.cs`  
**Class:** `HeartManager : SingletonAutoCreate<HeartManager>`

### 23.1. Hằng số

- `MAX_HEARTS = 5`
- `REGEN_SECONDS = 600` (10 phút / tim)

### 23.2. Property

| Property | Mô tả |
|----------|-------|
| `Hearts` | `UserData.hearts` |
| `MaxHearts` | 5 |
| `IsFull` | Hearts >= 5 |
| `SecondsUntilNextHeart` | Tính từ `UserData.nextHeartUnixTicks` |

### 23.3. Hàm

| Hàm | Mục đích |
|-----|----------|
| `TryConsume(int amount=1)` | Trừ tim, return false nếu không đủ |
| `Add(int amount, bool clampToMax=true)` | Cộng tim |
| `CatchUpRegen()` | Tính bù tim đã regen khi offline |
| `Persist()` | `SaveDataManager.Save()` + `UserDataFirebaseManager.UpdateHeart()` + fire `OnHeartsChanged` |

### 23.4. Hook lifecycle

- `Update()` — check `DateTime.UtcNow.Ticks` để regen.
- `OnApplicationFocus(bool)` / `OnApplicationPause(bool)` — catch up regen khi resume.

### 23.5. Công thức regen

```
Nếu Hearts < MaxHearts và nextHeartUnixTicks <= now:
   earned = (now - nextHeartUnixTicks) / REGEN_SECONDS
   Loop earned lần: Hearts++, nextHeartUnixTicks += REGEN_SECONDS
Nếu Full → nextHeartUnixTicks = 0
```

---

## 24. UserData & SaveDataManager

### 24.1. UserData (in-memory)

**File:** `Assets/Scripts/UserData/UserData.cs`

```csharp
public static class UserData {
    public const int MAX_HEARTS    = 5;
    public const int REGEN_SECONDS = 600;

    public static int  coin                  = 99999;  // test default
    public static int  level                 = 1;
    public static List<BoosterCounter> listBoosterCounters;
    public static int  hearts                = MAX_HEARTS;
    public static long nextHeartUnixTicks    = 0;
}
```

### 24.2. SaveDataManager

**File:** `Assets/Scripts/UserData/SaveDataManager.cs`

```
Save path: Application.persistentDataPath / UserData.json
   Android: /data/data/<pkg>/files/UserData.json
   Editor : C:\Users\<u>\AppData\LocalLow\<co>\<prod>\UserData.json
```

```csharp
[Serializable]
public class PlayerData {
    public int  coin;
    public int  level;
    public List<BoosterCounter> listBoosterCounters;
    public int  hearts;
    public long nextHeartUnixTicks;
}
```

| Hàm | Hành vi |
|-----|---------|
| `Save()` | Serialize `UserData.*` → JsonUtility.ToJson → file |
| `Load()` | Đọc file → gán vào `UserData.*`; nếu chưa có file → seed |
| `DeleteSave()` | Xóa file |

### 24.3. Seed mặc định lúc load lần đầu

```csharp
listBoosterCounters = new List<BoosterCounter> {
    new() { name = "Undo",    count = 2 },
    new() { name = "Add",     count = 2 },
    new() { name = "Shuffle", count = 2 },
    new() { name = "Magnet",  count = 2 }
};
```

Sau load: gọi `HeartManager.CatchUpRegen()`.

---

## 25. Firebase Integration

### 25.1. Cấu hình

- SDK: `Assets/Firebase/`
- EDM4U: `Assets/ExternalDependencyManager/`
- Cấu hình: `google-services.json`, `google-services-desktop.json`

### 25.2. FireBaseInit (test)

**File:** `Assets/Scenes/FireBaseInit.cs`

- Demo gọi Firestore với userId hardcode `"Son28112004"`.
- Chỉ dùng trong scene `TestFireBase.unity`.

### 25.3. UserDataFirebaseManager (chính)

**File:** `Assets/Scenes/UserDataFirebaseManager.cs` (~1.4k dòng)  
**Class:** `UserDataFirebaseManager : SingletonDDOL<UserDataFirebaseManager>`

#### Khởi tạo
```csharp
FirebaseApp.CheckAndFixDependenciesAsync()
   .ContinueWithOnMainThread(task => {
       db = FirebaseFirestore.DefaultInstance;
       IsFirebaseInitialized = true;
       CheckAndInitializeUser();
   });
```

#### Realtime Database
```
URL: https://blockjam3d-2072f-default-rtdb.asia-southeast1.firebasedatabase.app

Paths theo dõi:
   friend_requests/<myUserId>/<fromUserId>   ← yêu cầu kết bạn đến
   friend_accept/<myUserId>/<toUserId>       ← bạn đã chấp nhận
   friend_decline/<myUserId>/<toUserId>      ← bạn đã từ chối
   send_booster/<myUserId>/<pushKey>         ← nhận quà booster
```

#### Firestore collection `UserData/<userId>`

```json
{
  "Id": "10000001",
  "Name": "Player10000001",
  "Coin": 30,
  "Level": 1,
  "Heart": 5,
  "Frame": 0,
  "Boosters": [
    { "name": "Undo",    "count": 2 },
    { "name": "Add",     "count": 2 },
    { "name": "Shuffle", "count": 2 },
    { "name": "Magnet",  "count": 2 }
  ],
  "CreatedAt": ServerTimestamp,
  "UnityPlayerId": "...",
  "LoginType": "Local" | "Google",
  "LastLoginAt": ServerTimestamp
}
```

Subcollection: `UserData/<userId>/Friends/<friendId>` = `{ Id, CreatedAt }`.

#### Cấp ID

```
GenerateUniqueId():
   Firestore transaction trên ServerConfigs/UserCounter
   Atomic increment: LastUserID = max(10000000, LastUserID + 1)
   Fallback offline: timestamp >= 10000000
```

PlayerPrefs lưu `PlayerID`.

#### API chính

| Hàm | Mục đích |
|-----|----------|
| `CheckAndInitializeUser()` | Tạo/sync user |
| `SaveUserData(docId, data)` | Merge document |
| `UpdateHeart(int)` | Đồng bộ tim |
| `GetUserData(docId, cb)` | Lấy document |
| `GetAllUsers(cb)` | Cho leaderboard global |
| `GetFriendsList(userId, cb)` | Lấy subcol Friends |
| `SendFriendRequest(from, to)` | Tạo doc `FriendRequests` + RTD notify |
| `AcceptFriendRequest(from, to)` | Thêm vào Friends, xóa request |
| `SearchUsersByIdPrefix(prefix, cb)` | Tìm user theo prefix |
| `SendBooster(from, to, name, amount)` | Firestore transaction + RTD push (giới hạn 3 lần/ngày) |
| `LinkGoogleAccount()` | Link tài khoản Google qua Unity Auth |

#### Giới hạn gửi quà
- Tối đa **3 lần / ngày**, timezone **UTC+7**.
- Lưu state: `LastSendBoosterDate`, `SendBoosterCount`.
- Heart cũng nằm trong limit này.

---

## 26. Leaderboard & Friend System

Thư mục: `Assets/Scripts/UI/Screens/ScreenRanking/`

### 26.1. LeaderBoardManager (76 dòng)

```csharp
public static Action onUpdateFriendList;
public static Action onUpdatePlayerList;
```

2 tab: Friends / Players.

### 26.2. LeaderBoardPlayerController

- `LoadListPlayer()` → `GetAllUsers()` → sort desc theo Level → rank 1..N.
- Player hiện tại dùng prefab `LeaderBoardUserInforPlayer`, người khác `LeaderBoardUserInfor`.

### 26.3. LeaderBoardFriendController

- Danh sách bạn: friends + self (self luôn cuối).
- **Add Friend panel:** search theo prefix ID, lọc bỏ chính mình + bạn cũ + đã gửi request.
- **Friend Requests panel:** danh sách pending, có nút Accept / Decline.
- **Copy ID:** copy `PlayerPrefs["PlayerID"]` vào clipboard.

### 26.4. Friend flow

```
SendFriendRequest A→B
├─ Firestore: FriendRequests/<docId> { From:A, To:B, Status:"Pending" }
└─ RTD push: friend_requests/B/A

B.AcceptFriendRequest()
├─ Firestore: UserData/A/Friends/B = { Id:B, CreatedAt }
├─ Firestore: UserData/B/Friends/A = { Id:A, CreatedAt }
├─ Xóa FriendRequests
└─ RTD push: friend_accept/A/B  → A nghe được, refresh list

B.DeclineFriendRequest()
├─ Xóa FriendRequests
└─ RTD push: friend_decline/A/B
```

Sau khi nhận thông báo RTD → tự động xóa node để khỏi nhận lại.

---

## 27. Sơ đồ quan hệ tổng thể

### 27.1. Lớp managers

```
                       ┌──────────────────────┐
                       │   GameManager (DDOL) │
                       │   State machine      │
                       └────────┬─────────────┘
                                │ ChangeState
                ┌───────────────┼───────────────┐
                ▼               ▼               ▼
        ┌────────────┐   ┌────────────┐  ┌────────────┐
        │ UIManager  │   │LevelManager│  │ Audio/etc. │
        │ (scene)    │   │ (scene)    │  │            │
        └─────┬──────┘   └─────┬──────┘  └────────────┘
              │                │
       Screens & Popups        │
              │                ▼
              │      ┌──────────────────────────────┐
              │      │ BoardCtrl                    │
              │      │ CellPlayCtrl                 │
              │      │ BoosterCtrl (Undo/Add/Shuf/  │
              │      │              Magnet)         │
              │      │ FindingPath                  │
              │      │ TutorialCtrl                 │
              │      │ GridSpotSpawn list           │
              │      └──────────────────────────────┘
              │
              ▼
   ┌────────────────────────────────────────────────────┐
   │ Singleton DDOL (xuyên scene):                      │
   │  ┌────────────────┐ ┌──────────────────────────┐   │
   │  │ Addressable    │ │ CustomeEventSystem       │   │
   │  │ Manager        │ │ (event bus pub/sub)      │   │
   │  └────────────────┘ └──────────────────────────┘   │
   │  ┌────────────────┐ ┌──────────────────────────┐   │
   │  │ AudioManager   │ │ UserDataFirebaseManager  │   │
   │  └────────────────┘ └──────────────────────────┘   │
   │  ┌────────────────┐                                │
   │  │ HeartManager   │                                │
   │  └────────────────┘                                │
   └────────────────────────────────────────────────────┘

   ┌────────────────────────────────────────────────────┐
   │ Static / utility:                                  │
   │  • UserData (in-memory)                            │
   │  • SaveDataManager (JsonUtility I/O)               │
   └────────────────────────────────────────────────────┘
```

### 27.2. Dòng dữ liệu chính

```
        ┌────────────────────┐
        │ Player click block │
        └──────────┬─────────┘
                   ▼
        ┌────────────────────┐
        │ ItemClickCtrl      │
        └──────────┬─────────┘
                   │
       ┌───────────┼───────────────┐
       ▼           ▼               ▼
   FindingPath  BoardCtrl   BoosterUndo.AddStack
   .BFSFind    .UpdateBoardCell
       │
       ▼
   BoardCellMovement.MovementPath(path)
       │
       ▼
   CellPlayCtrl.CheckAndSaveBoardCell
       │
       ▼
   CustomeEventSystem.CheckMatch_3
       │
       ▼
   CellPlayCtrl.Match_3 → Match3Process
       │
       ▼
   SetAnimMerge → boardCellMatch_3 → BlockItemSpawner pool

   checkWin / checkLose
       │
       ▼
   GameManager.WinGame / LoseGame
       │
       ▼
   UIManager.ShowPopup<PopupWinGame / PopupLoseGame>
```

### 27.3. Đồng bộ Firebase

```
Local change (coin, hearts, booster)
     │
     ├─ UserData.* (in-memory)
     ├─ SaveDataManager.Save() → JSON local
     └─ UserDataFirebaseManager.UpdateHeart() / SaveUserData()
                │
                ▼
          Firestore UserData/<id>
                │
        ┌───────┴────────┐
        ▼                ▼
   Leaderboard       Friends subcol
   (GetAllUsers)     (GetFriendsList)


Realtime DB notifications
   friend_requests/<me>/<from>     → popup yêu cầu kết bạn
   friend_accept/<me>/<to>         → refresh friend list
   friend_decline/<me>/<to>        → notify
   send_booster/<me>/<pushKey>     → cộng booster, save
```

---

## PHỤ LỤC A — TypeItem enum

```
BlueBase, BrownBase, GreenBase, MagentaBase, RedBase, PurpleBase, YellowBase
```

Số ánh xạ trong `prefabNames`:  
`"1" → BlueBase, "2" → BrownBase, "3" → GreenBase, "4" → MagentaBase, "5" → RedBase, "6" → PurpleBase, "7" → YellowBase`.  
Hậu tố `B` (vd `"1B"`) = block bị **barrel** chặn.

---

## PHỤ LỤC B — Quy ước Addressable label

| Label | Asset type | Cache trong AddressableManager |
|-------|------------|--------------------------------|
| `Wall` | GameObject | `_cachedPrefabs[name]` |
| `Container` | GameObject | `_cachedPrefabs[name]` |
| `Block` | GameObject (block 7 màu + barrel) | `_cachedPrefabs[name]` |
| `GridSpot` | GameObject (máng các hướng) | `_cachedPrefabs[name]` |
| `Level` | LevelData SO | `_cachedLevelGroups[baseName]` |

---

## PHỤ LỤC C — Quy ước commit gần đây

```
fe7d0ef  fix: friend list disappear when switch tab
6029f33  add leaderboard
4a44c1c  add text
503286d  add firebase
1e4ddb0  init
```

---

## PHỤ LỤC D — Lưu ý khi mở rộng

1. **Thêm prefab block / wall mới** → tạo prefab, **bắt buộc gán Addressable label** ("Block" / "Wall" / "Container" / "GridSpot"), tên address khớp với chuỗi trong `LevelData.prefabNames[]`.
2. **Thêm level mới** → tạo asset `Level_X(board_1).asset`, `Level_X(board_2).asset`, `Level_X(board_3).asset`, gán label "Level". AddressableManager tự gom theo `Level_X` và sort theo `board_N`.
3. **Thêm booster mới** → thêm class theo pattern `BoosterXxx + BoosterXxxPos`; mở rộng `BoosterCtrl`; cập nhật `ActiveBooster` event size (hiện 4 phần tử).
4. **Đổi giá tim** → sửa `COIN_COST_PER_HEART` trong `PopupAddHeart.cs`. Lưu ý đồng bộ với quy ước thiết kế (memory ghi: cost = win-coin-reward × 10 — hiện code đang là 500 trong khi win reward = 100 nên cần thống nhất).
5. **Sửa số round / level** → đổi điều kiện `Round > 2` trong `LevelManager.NextRoundLevel()` và `CellPlayCtrl.checkWin()`.
6. **Thay engine event** → tất cả thay đổi UI nên đi qua `CustomeEventSystem` để giữ loose-coupling.

---

> 📌 Tài liệu này tự sinh từ phân tích source code thực tế tại commit `fe7d0ef`. Nếu code thay đổi (rename, refactor), nên cập nhật lại các section liên quan đặc biệt là wireframe UI và bảng API.
