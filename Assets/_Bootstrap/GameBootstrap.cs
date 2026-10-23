using System;
using System.IO;
using CursedDeck.Data;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameContent content;
        [SerializeField] private InputActionAsset inputActions;
        private RunSaveStore store;
        private GameHost host;
        private EventBus bus;
        private GameSettings settings;
        private GameObject canvasObject;
        private InputActionAsset runtimeActions;
        private readonly InputActionReference[] references=new InputActionReference[10];
        private int referenceCount;
        public GameHost Host => host;
        public GameView View { get; private set; }
        public string SavePath { get; private set; }
        public string StorageDirectory {get;set;}
        private void Start()
        {
            try
            {
                if(content==null) content=Resources.Load<GameContent>("CursedDeck/Content");
                content.Validate(); Screen.orientation=ScreenOrientation.Portrait;
                settings=new GameSettings {Sound=PlayerPrefs.GetInt("cursed.sound",1)!=0,Haptics=PlayerPrefs.GetInt("cursed.haptics",1)!=0,
                    LeftHanded=PlayerPrefs.GetInt("cursed.left",0)!=0,Colorblind=PlayerPrefs.GetInt("cursed.colorblind",0)!=0};
#if UNITY_EDITOR
                SavePath=Path.GetFullPath(Path.Combine(Application.dataPath,"../TestResults/editor-run.json"));
#else
                SavePath=Path.Combine(Application.persistentDataPath,"cursed-deck-run-v1.json");
#endif
                if(!string.IsNullOrEmpty(StorageDirectory))SavePath=Path.Combine(StorageDirectory,"cursed-deck-run-v1.json");
                Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
                store=new RunSaveStore(SavePath);store.CorruptionRecovery=ReportCorruption;
                InitializeMeta();InitializeTutorialSettings();
                var lifecycle=new RunLifecycle(store,42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout());
                var resumed=validatedProfileCheckpoint??lifecycle.Controller;
                if(lifecycle.Warning!=null){recoveryPending=true;recoveryDefault|=!lifecycle.Resumed && validatedProfileCheckpoint==null;}
                bus=new EventBus(); host=new GameHost(resumed,bus,MetaPresentations(),Preferences()); host.Notice=profile.Active?.RunCheckpoint==null?lifecycle.Warning:null;
                host.MetaRequested+=MetaCommand; InitializeServices(); host.ServiceRequested+=ServiceCommand;
                EnsureMetaSession();
                host.SaveRequested+=SaveNow; host.NewRunRequested+=NewRun; host.MenuNewRunRequested+=NewMenuRun; host.MenuDailyRequested+=NewDailyMenuRun; host.SettingsRequested+=ToggleSetting;host.TutorialRequested+=StartTutorial;
                SetupInput();
                var boardPrefab=Resources.Load<GameView>("UI/BoardView");
                canvasObject=boardPrefab!=null?Instantiate(boardPrefab).gameObject:new GameObject("CursedDeckCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(GameView));
                View=canvasObject.GetComponent<GameView>(); View.Initialize(bus,content.Font,content.CardFront,content.Characters.DefaultCardBack!=null?content.Characters.DefaultCardBack:content.CardBack,content.SuitIcons,Resources.Load<CardView>("TCG/CardView"),CharacterVisuals());
                InjectRelicIcons();
                View.QueueTutorial(false);
                host.ShowStartMenu(profile.Active?.RunCheckpoint!=null || lifecycle.Resumed);
                Canvas.ForceUpdateCanvases(); host.Publish(GameFeedback.Deal); PublishMeta(); InitializeAudio();
                if(recoveryPending)View.ShowRecovery(RecoveryMessage());
                else if(!settings.hasCompletedTutorial)bus.Send(new GameCommand(GameCommandKind.StartTutorial));
            }
            catch(Exception ex) { Debug.LogException(ex); }
        }
        private UiSettings Preferences() => new UiSettings(settings.Sound,settings.Haptics,settings.LeftHanded,settings.Colorblind,settings.reduceMotion);
        private void ToggleSetting(GameCommandKind kind)
        {
            switch(kind)
            {
                case GameCommandKind.ToggleSound: settings.Sound=!settings.Sound; break;
                case GameCommandKind.ToggleHaptics: settings.Haptics=!settings.Haptics; break;
                case GameCommandKind.ToggleLeftHanded: settings.LeftHanded=!settings.LeftHanded; break;
                case GameCommandKind.ToggleColorblind: settings.Colorblind=!settings.Colorblind; break;
                case GameCommandKind.ToggleReduceMotion: settings.reduceMotion=!settings.reduceMotion; break;
            }
            PlayerPrefs.SetInt("cursed.sound",settings.Sound ? 1 : 0); PlayerPrefs.SetInt("cursed.haptics",settings.Haptics ? 1 : 0);
            PlayerPrefs.SetInt("cursed.left",settings.LeftHanded ? 1 : 0); PlayerPrefs.SetInt("cursed.colorblind",settings.Colorblind ? 1 : 0); PlayerPrefs.Save();
            profile.Settings=settings;metaStore.Save(profile);host.UpdateSettings(Preferences());
        }
        private void NewRun(int seed)
        {
            int next=seed==0 ? Environment.TickCount : seed;
            StartMetaRun(next,"classic",null);
        }
        private void NewMenuRun()
        {
            View.QueueTutorial(false);
            StartMetaRun(Environment.TickCount,"classic",null,true);
        }
        private void NewDailyMenuRun()
        { View.QueueTutorial(false);var now=DateTime.UtcNow;StartMetaRun(DailyRun.Seed(now),"classic",DailyRun.DateKey(now),true); }
        private void SaveNow()
        {
            if(host==null || store==null || !host.HasEnteredRun || host.IsTutorial) return;
            if(!recoveryWritable || metaStore.SaveBlocked){host.Notice="Damaged save could not be preserved. Saving is paused until storage is available or progress is reset.";return;}
            try { ObserveMeta(); store.Save(host.Controller); analytics?.Track("checkpoint",host.Controller.State.Seed,host.Controller.State.Floor,host.Controller.State.Round,host.Controller.CurrentRound.State.Score); }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            { host.Notice="Checkpoint could not be saved. Please try again."; Debug.LogWarning("Cursed Deck save: "+ex.Message); }
        }
        private InputActionReference Action(string name)
        {
            var reference=InputActionReference.Create(runtimeActions.FindAction("UI/"+name,true));
            references[referenceCount++]=reference; return reference;
        }
        private void SetupInput()
        {
            if(EventSystem.current!=null) return;
            var system=new GameObject("EventSystem",typeof(EventSystem)); var module=system.AddComponent<InputSystemUIInputModule>(); module.enabled=false;
            if(inputActions!=null)
            {
                runtimeActions=Instantiate(inputActions); module.actionsAsset=runtimeActions;
                module.point=Action("Point"); module.leftClick=Action("Click"); module.scrollWheel=Action("ScrollWheel");
                module.move=Action("Navigate"); module.submit=Action("Submit"); module.cancel=Action("Cancel");
            }
            else module.AssignDefaultActions();
            module.enabled=true;
        }
        private void Update() { if(View!=null && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)View.NavigateBack(); }
        private void OnApplicationPause(bool paused) { if(paused){SaveNow();if(host!=null)bus.Send(new GameCommand(GameCommandKind.Pause));} }
        private void OnApplicationFocus(bool focus) { if(!focus) SaveNow(); }
        private void OnApplicationQuit() { SaveNow(); }
        private void OnDestroy()
        {
            SaveNow();
            if(host!=null) { host.ServiceRequested-=ServiceCommand; host.MetaRequested-=MetaCommand; host.SaveRequested-=SaveNow; host.NewRunRequested-=NewRun; host.MenuNewRunRequested-=NewMenuRun; host.MenuDailyRequested-=NewDailyMenuRun; host.SettingsRequested-=ToggleSetting;host.TutorialRequested-=StartTutorial; host.Dispose(); }
            if(canvasObject!=null) Destroy(canvasObject);
            for(int i=0;i<referenceCount;i++) if(references[i]!=null) Destroy(references[i]);
            if(runtimeActions!=null) Destroy(runtimeActions);
        }
    }
}
