# Birdie — Architecture Documentation

## 1. System Overview

Birdie is a cozy desktop companion game built in Unity 6. The game runs as a transparent overlay just above the Windows taskbar. The architecture follows a **Manager-centric pattern**: a singleton `GameManager` orchestrates all subsystems, each of which is a self-contained `BaseManager` subclass.

```mermaid
graph TB
    subgraph "Unity Runtime"
        GM["🎮 GameManager\n(Singleton Orchestrator)"]
    end

    subgraph "Core Systems"
        BM["BirdManager\nSpawning & Lifecycle"]
        EM["EconomyManager\nGolden Seeds"]
        FM["FriendshipManager\nPer-bird Relationship"]
        ENV["EnvironmentManager\nInteractive Objects"]
        SM["SoundManager\nAudio Channels"]
    end

    subgraph "Content Systems"
        DM["DiaryManager\nBird Discoveries"]
        STM["StoreManager\nShop & Purchases"]
        WM["WindowsillManager\nHabitat Items"]
        DMM["DailyMissionManager\nQuests"]
    end

    subgraph "UI Systems"
        MM["MenuManager\nMenu Routing"]
        DUI["DiaryUIManager\nDiary View"]
        TM["ToastManager\nNotifications"]
        LM["LoadingManager\nLoading Screen"]
        HGC["HideGameController\nOverlay Visibility"]
    end

    subgraph "Persistence"
        SVM["SaveManager\nJSON + Backup"]
        SD["SaveData\nMaster Container"]
    end

    subgraph "Data"
        BD["BirdData\nScriptableObjects"]
        SL["SoundLibrary\nScriptableObject"]
        SID["StoreItemData\nScriptableObjects"]
    end

    GM --> BM & EM & FM & ENV & SM
    GM --> DM & STM & WM & DMM
    GM --> MM & DUI & TM & LM & HGC
    GM --> SVM

    SVM --> SD
    BM --> BD
    SM --> SL
    STM --> SID
```

---

## 2. Initialization Pipeline

The game uses a **phased async initialization** via UniTask. Each phase must complete before the next starts. Within a phase, independent managers initialize in parallel.

```mermaid
flowchart TD
    START([App Start]) --> LOAD[LoadingManager\nShow Loading Screen]
    LOAD --> P1

    subgraph P1["Phase 1 — Persistence"]
        SVM[SaveManager.Initialize\nLoad save file / restore backup]
    end

    P1 --> P2

    subgraph P2["Phase 2 — Core Managers"]
        direction LR
        subgraph PARALLEL["Parallel"]
            BM2[BirdManager]
            EM2[EconomyManager]
            FM2[FriendshipManager]
            ENV2[EnvironmentManager]
            SM2[SoundManager]
            TM2[ToastManager]
            WM2[WindowsillManager]
        end
        subgraph SEQ["Sequential (after parallel)"]
            DM2[DiaryManager\nneeds BirdManager]
            STM2[StoreManager\nneeds Economy + Save]
            DUI2[DiaryUIManager\nneeds Diary + Friendship]
            MM2[MenuManager\nneeds all above]
            DMM2[DailyMissionManager\nsubscribes to events]
        end
        PARALLEL --> SEQ
    end

    P2 --> P3

    subgraph P3["Phase 3 — Game Start"]
        GS[GameManager.StartGame\nSet state to Playing]
        BS[BirdManager.StartSpawning\nBegin spawn loop]
    end

    P3 --> HIDE[LoadingManager\nHide Loading Screen]
    HIDE --> RUNNING([Game Running])
```

---

## 3. Manager Class Hierarchy

```mermaid
classDiagram
    class BaseManager {
        <<abstract>>
        +Initialize() UniTask
        +IsInitialized bool
    }

    class GameManager {
        +Instance GameManager
        +CurrentGameState GameState
        +OnGameStateChanged Action~GameState~
        +OnMenuOpened Action~MenuType~
        +OnGameStarted Action
        +OnMinigameStarted Action
        -InitializeAsync() UniTask
        -StartGame()
    }

    class BirdManager {
        +SpawnBird()
        +PauseAllBirds()
        +ResumeBirdSpawning()
        -WeightedBirdSelection() BirdData
        -SpawnLoopAsync() UniTask
    }

    class EconomyManager {
        +GoldenSeeds int
        +OnGoldenSeedsChanged Action~int~
        +AddGoldenSeeds(amount)
        +CanAfford(cost) bool
        +TryPurchase(cost) bool
    }

    class FriendshipManager {
        +GetFriendshipLevel(birdId) int
        +AddFriendshipPoints(birdId, points)
        +OnFriendshipLevelUp Action~string,int~
    }

    class EnvironmentManager {
        +GetObjectsOfType(type) List~BirdObject~
        +GetNearestObject(pos, type) BirdObject
        +HasObjectOfType(type) bool
        -RegisterObject(obj)
        -UnregisterObject(obj)
    }

    class SoundManager {
        +PlaySFX(clip)
        +PlayMusic(clip)
        +PlayAmbient(clip)
        +SetVolume(channel, volume)
        +SetMute(channel, muted)
    }

    class SaveManager {
        +SaveData SaveData
        +SaveGame()
        +LoadGame()
        -CreateBackup()
        -RestoreFromBackup()
    }

    BaseManager <|-- BirdManager
    BaseManager <|-- EconomyManager
    BaseManager <|-- FriendshipManager
    BaseManager <|-- EnvironmentManager
    BaseManager <|-- SoundManager
    BaseManager <|-- DiaryManager
    BaseManager <|-- StoreManager
    BaseManager <|-- WindowsillManager
    BaseManager <|-- DailyMissionManager
    BaseManager <|-- MenuManager
    BaseManager <|-- ToastManager
    GameManager --> BaseManager : orchestrates
    GameManager --> SaveManager : phase 1
```

---

## 4. Bird Lifecycle State Machine

Each bird goes through a fixed lifecycle from spawn to destruction. Visiting is the main state where behavior selection happens. All states can transition to Paused during a minigame.

```mermaid
stateDiagram-v2
    [*] --> Appearing : BirdManager.SpawnBird()

    Appearing : Appearing
    Appearing : ArrivingBehavior executes
    Appearing : Fly-in animation plays

    Visiting : Visiting
    Visiting : Behavior loop runs
    Visiting : Visit timer counts down
    Visiting : ScanEnvironment() detects objects

    Leaving : Leaving
    Leaving : LeavingBehavior executes
    Leaving : Fly-out animation plays

    Paused : Paused
    Paused : Animator speed = 0
    Paused : Timer frozen

    Destroyed : [*]

    Appearing --> Visiting : ArrivingBehavior completes
    Visiting --> Leaving : Visit timer expires\nor ForcedLeave()
    Leaving --> Destroyed : Animation complete\nGameObject.Destroy()

    Visiting --> Paused : BirdManager.PauseAllBirds()\n(minigame starts)
    Paused --> Visiting : BirdManager.ResumeBirdSpawning()\n(minigame ends, timer recalculated)
```

---

## 5. Bird Behavior System

During the Visiting state, the bird cycles through behaviors selected by a weighted random system. Each behavior is a `ScriptableObject` that encapsulates its own logic.

### 5a. Class Structure

```mermaid
classDiagram
    class BirdBehaviorState {
        <<abstract ScriptableObject>>
        +OnEnter(bird) void
        +Execute(bird) UniTask
        +OnExit(bird) void
        +CanExecute(bird) bool
        +CalculateWeight(bird) float
    }

    class BirdBehaviorEntry {
        +Behavior BirdBehaviorState
        +BaseWeight float
    }

    class BirdData {
        <<ScriptableObject>>
        +BirdName string
        +BirdID string
        +Rarity BirdRarity
        +BaseSpawnWeight int
        +AppearanceTimeRange TimeRange
        +PossibleBehaviors List~BirdBehaviorEntry~
        +ArrivingBehavior BirdBehaviorState
        +LeavingBehavior BirdBehaviorState
        +VisitDurationMin float
        +VisitDurationMax float
        +ObjectBonusSeconds float
        +MaxFriendshipLevel int
        +SongParts List~AudioClip~
        +PossibleGifts List~GiftItem~
    }

    class Bird {
        +BirdData Data
        +CurrentState BirdState
        +Initialize(data, spawnPos, landPos)
        +StartVisitAsync() UniTask
        -TransitionToNextBehavior()
        -ScanEnvironment()
        -Pause()
        -Resume()
    }

    BirdBehaviorState <|-- ArrivingBehavior
    BirdBehaviorState <|-- IdleBehavior
    BirdBehaviorState <|-- WalkingRandomlyBehavior
    BirdBehaviorState <|-- SingingBehavior
    BirdBehaviorState <|-- EatingBehavior
    BirdBehaviorState <|-- BathingBehavior
    BirdBehaviorState <|-- LeavingBehavior
    BirdBehaviorState <|-- FlyingBehaviorBase

    BirdData "1" *-- "many" BirdBehaviorEntry
    BirdBehaviorEntry --> BirdBehaviorState
    Bird --> BirdData
    Bird --> BirdBehaviorState : currentBehavior
```

### 5b. Behavior Selection Flow

```mermaid
flowchart TD
    LOOP[Visiting Loop] --> CHECK{Current\nbehavior done?}
    CHECK -- No --> EXEC[Execute current behavior]
    EXEC --> LOOP

    CHECK -- Yes --> EXIT[OnExit current behavior]
    EXIT --> GATHER[Gather candidateBehaviors\nfrom BirdData.PossibleBehaviors]
    GATHER --> FILTER["Filter: CanExecute(bird) == true"]
    FILTER --> WEIGHT["Calculate weight for each:\nBaseWeight × CalculateWeight(bird)"]
    WEIGHT --> SELECT[Weighted Random Pick]
    SELECT --> ENTER[OnEnter new behavior]
    ENTER --> LOOP

    style LOOP fill:#2d6a4f,color:#fff
    style SELECT fill:#1d3557,color:#fff
```

---

## 6. Interactive Objects System

Birds are attracted to interactive objects on the windowsill. Objects auto-register with `EnvironmentManager` and expose interaction positions for behaviors.

```mermaid
classDiagram
    class BirdObject {
        <<abstract MonoBehaviour>>
        +ObjectID string
        +ObjectType BirdObjectType
        +Attractiveness float
        +InteractionPosition Vector3
        +InteractingBirdCount int
        +OnClicked() void
        -OnEnable() void
        -OnDisable() void
    }

    class BirdFeeder {
        +FoodLevel float
        +HasFood bool
        +ConsumeFood()
    }

    class BirdBath {
        +WaterLevel float
        +HasWater bool
    }

    class TrashItem {
        +PickUp()
    }

    class EnvironmentManager {
        -registeredObjects List~BirdObject~
        +GetObjectsOfType(type) List~BirdObject~
        +GetNearestObject(pos, type) BirdObject
        +HasObjectOfType(type) bool
        +RegisterObject(obj)
        +UnregisterObject(obj)
    }

    class BirdObjectType {
        <<enumeration>>
        Feeder
        BirdBath
        Toy
        Decoration
        Perch
    }

    BirdObject <|-- BirdFeeder
    BirdObject <|-- BirdBath
    BirdObject <|-- TrashItem

    BirdObject --> BirdObjectType
    BirdObject --> EnvironmentManager : registers on Enable
    EnvironmentManager "1" o-- "many" BirdObject : registry

    EatingBehavior ..> EnvironmentManager : GetNearestObject(Feeder)
    BathingBehavior ..> EnvironmentManager : GetNearestObject(BirdBath)
```

---

## 7. Spawn System & Weighting

`BirdManager` uses a multi-factor weighted selection to determine which bird spawns next.

```mermaid
flowchart LR
    subgraph FILTER["1. Filter candidates"]
        TG["Time gate:\nAppearanceTimeRange\ncovers current hour"]
        REQ["Requirements:\nRequiredUpgradeIDs\nall purchased"]
        HAB["Habitat level\n>= MinimumHabitatLevel"]
    end

    subgraph WEIGHT["2. Calculate spawn weight"]
        BASE["BaseSpawnWeight\n(1–100)"]
        PITY["Pity bonus:\n+weight per tick\nsince last seen"]
        FRIEND["Friendship bonus:\nhigher level → slightly\nhigher weight"]
    end

    subgraph SELECT["3. Selection"]
        WRS["Weighted Random\nSelection"]
        MAX["Respect MaxConcurrentBirds\n(default: 1)"]
    end

    FILTER --> WEIGHT --> SELECT --> SPAWN[Spawn Bird\nat BirdSpawnPoints]
```

---

## 8. Save Data Model

All game state is serialized to JSON via `SaveManager`. A backup is created before every save.

```mermaid
classDiagram
    class SaveData {
        +lastSaveDateString string
        +IsValid() bool
        +economy EconomySaveData
        +audio AudioSaveData
        +diary DiarySaveData
        +friendship FriendshipSaveData
        +windowsill WindowsillSaveData
        +dailyMissions DailyMissionSaveData
    }

    class EconomySaveData {
        +goldenSeeds int
    }

    class AudioSaveData {
        +masterVolume float
        +sfxVolume float
        +musicVolume float
        +ambientVolume float
        +masterMuted bool
        +sfxMuted bool
        +musicMuted bool
        +ambientMuted bool
    }

    class DiarySaveData {
        +discoveredBirds List~string~
        +totalEncounters Dictionary~string,int~
        +firstSeenDates Dictionary~string,string~
    }

    class FriendshipSaveData {
        +friendshipLevels Dictionary~string,int~
        +friendshipPoints Dictionary~string,int~
    }

    class WindowsillSaveData {
        +ownedItems List~string~
        +itemPositions List~ItemPositionEntry~
    }

    class DailyMissionSaveData {
        +activeMissions List~MissionProgress~
        +lastRefreshDate string
    }

    class ItemPositionEntry {
        +itemId string
        +position Vector3
    }

    SaveData *-- EconomySaveData
    SaveData *-- AudioSaveData
    SaveData *-- DiarySaveData
    SaveData *-- FriendshipSaveData
    SaveData *-- WindowsillSaveData
    SaveData *-- DailyMissionSaveData
    WindowsillSaveData *-- ItemPositionEntry
```

### Save file location

```
%AppData%\..\LocalLow\<Company>\Birdie\
├── save.json
└── save_backup.json   ← created before every write
```

---

## 9. Economy & Progression Flow

The game uses a **dual-resource economy** to separate exploration (Golden Seeds) from emotional attachment (Friendship Points).

```mermaid
flowchart TD
    subgraph INTERACT["Player Interaction"]
        MG["Play Minigame\n(10–30 seconds)"]
        GIFT["Receive Gift\n(Friendship Level 4)"]
    end

    subgraph GOLDEN["Resource A: Golden Seeds 🌾"]
        GS_EARN["Earn Golden Seeds\n(minigame reward)"]
        GS_SPEND["Spend in Shop\n→ Feeders, Baths, Toys"]
        NEW_BIRD["New species\ncan now spawn"]
    end

    subgraph FRIEND["Resource B: Friendship Points ❤️"]
        FP_EARN["Earn Friendship Points\n(per-bird, per interaction)"]
        LVL1["Level 1 — Contact\nName + Photo → Diary"]
        LVL2["Level 2 — Acquaintance\nDiet + Habitat info"]
        LVL3["Level 3 — Friend\nFun facts + Trivia"]
        LVL4["Level 4 — Best Friend\nBird leaves physical gifts"]
    end

    MG --> GS_EARN & FP_EARN
    GS_EARN --> GS_SPEND --> NEW_BIRD
    FP_EARN --> LVL1 --> LVL2 --> LVL3 --> LVL4
    LVL4 --> GIFT
```

---

## 10. UI & Menu System

```mermaid
flowchart TD
    HGC["HideGameController\nShow/Hide entire overlay"]

    MM["MenuManager\nRoutes open/close calls"]

    subgraph MENUS["Menus (MenuType enum)"]
        DIARY["Diary\nBird album + lore"]
        SHOP["Shop\nPurchase habitat items"]
        SETTINGS["Settings\nAudio + preferences"]
        MINIGAMES["Minigames\nGame selection"]
        MISSIONS["Daily Missions\nQuest tracker"]
        TUTORIAL["Tutorial"]
    end

    subgraph OVERLAYS["Overlays / HUD"]
        TOAST["ToastManager\nNotification popups"]
        LOADING["LoadingManager\nLoading screen"]
        CTX["BirdContextMenuUI\nRight-click on bird"]
        RES["ResourceBarTracker\nGolden Seeds display"]
    end

    subgraph MINIGAME_IMPL["Minigame Implementations"]
        SC["SeedCatcher"]
        SS["SeedSorting"]
        SIM["Simon Says"]
        SP["Sliding Puzzle"]
    end

    MM --> DIARY & SHOP & SETTINGS & MINIGAMES & MISSIONS & TUTORIAL
    MINIGAMES --> SC & SS & SIM & SP
    HGC --> MM
    TOAST -.->|IToastService| MM
```

### Minigame Interface

```mermaid
classDiagram
    class IMinigame {
        <<interface>>
        +GameClosed Action~MinigameResult~
        +FriendshipReward int
        +SetRewardTiers(tiers)
        +SetDifficulty(settings)
        +StartGame()
    }

    class SeedCatcherMinigame {
        +StartGame()
    }
    class SeedSortingMinigame {
        +StartGame()
    }
    class SimonSaysMinigame {
        +StartGame()
    }
    class SlidingPuzzleMinigame {
        +StartGame()
    }

    IMinigame <|.. SeedCatcherMinigame
    IMinigame <|.. SeedSortingMinigame
    IMinigame <|.. SimonSaysMinigame
    IMinigame <|.. SlidingPuzzleMinigame
```

---

## 11. Audio System

```mermaid
flowchart LR
    SL["SoundLibrary\nScriptableObject\nkey → AudioClip"]

    SM["SoundManager"]

    subgraph CHANNELS["Three independent AudioSources"]
        SFX["SFX Channel\none-shots"]
        MUS["Music Channel\nlooping background"]
        AMB["Ambient Channel\nlooping ambience"]
    end

    subgraph CONTROLS["Per-channel controls"]
        VOL["Volume (0–1)"]
        MUTE["Mute toggle"]
        MASTER["Master volume / mute"]
    end

    SL --> SM
    SM --> SFX & MUS & AMB
    CONTROLS --> SM
    SM --> AudioSaveData
```

---

## 12. Debug & Logging System

```mermaid
classDiagram
    class DebugBase {
        <<static>>
        +Log(category, msg)
        +LogWarning(category, msg)
        +LogError(category, msg)
        +LogException(category, ex)
    }

    class DebugManager {
        -enabledCategories HashSet~DebugCategory~
        +IsEnabled(category) bool
        +Enable(category)
        +Disable(category)
    }

    class DebugCategory {
        <<enumeration>>
        General
        Mouse
        UI
        Transparency
        Managers
        Birds
        Economy
        Friendship
        Camera
        Physics
        Audio
        Debug
    }

    DebugBase --> DebugManager : checks IsEnabled
    DebugBase --> DebugCategory
```

---

## 13. Key Design Decisions

| Decision | Rationale |
|---|---|
| Manager Singleton pattern via `GameManager` | Centralized initialization order, easy cross-system access |
| `BaseManager` abstract class | Uniform `Initialize() UniTask` contract, guarantees async init |
| `BirdBehaviorState` as ScriptableObject | Behaviors configurable per-species in the Unity Editor without code |
| Dual-resource economy | Separates horizontal (new birds) from vertical (deeper bonds) progression |
| JSON + backup save | Simple, human-readable, self-healing on corruption |
| UniTask over Coroutines | Proper async/await, cancellation tokens, no MonoBehaviour dependency |
| `EnvironmentManager` registry | Behaviors discover objects at runtime without hard references |
| `nameof()` in all logs | Refactor-safe — class renames don't silently break log filtering |
