using TMPro;
using UnityEngine;
using CursedDeck.Run;

namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private RectTransform startMenu,pauseMenu,newRunConfirm;
        private UnityEngine.UI.Button menuContinue,menuNew,menuSettings,menuStats,pauseResume,pauseSettings,pauseTitle,confirmNew,cancelNew;
        private TextMeshProUGUI checkpointLabel,pauseDetail;
        private bool queuedTutorial,tutorialSuspended,pendingDaily;
        private UnityEngine.UI.Button menuDaily,menuTutorial,settingsTutorial;
        public UnityEngine.UI.Button ReplayTutorialButton=>menuTutorial;
        public UnityEngine.UI.Button SettingsReplayTutorialButton=>settingsTutorial;
        public UnityEngine.UI.Button StartDailyChallengeButton=>menuDaily;
        public bool StartMenuVisible => startMenu!=null && startMenu.gameObject.activeSelf;
        public bool PauseMenuVisible => pauseMenu!=null && pauseMenu.gameObject.activeSelf;
        public UnityEngine.UI.Button StartNewRunButton => menuNew;
        public UnityEngine.UI.Button ResumeRunButton => menuContinue;
        public void QueueTutorial(bool needed) { queuedTutorial=needed;tutorialSuspended=false; }

        // Called by the existing prefab authoring path as well as runtime initialization.
        private void BuildMenuScreens()
        {
            startMenu=Panel("StartMenu");
            var backdrop=UIPackLayout.Panel("Backdrop",startMenu,skin!=null?skin.Background:null,Vector2.zero,Vector2.one);
            backdrop.type=UnityEngine.UI.Image.Type.Simple;
            var fit=backdrop.GetComponent<UnityEngine.UI.AspectRatioFitter>();if(fit==null)fit=backdrop.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=941f/1672;
            var shade=UIPackLayout.Panel("Shade",startMenu,null,Vector2.zero,Vector2.one);shade.color=new Color(.01f,.025f,.018f,.43f);
            var plaque=UIPackLayout.Panel("TitlePlaque",startMenu,skin!=null?skin.TitlePlaque:null,new Vector2(.04f,.775f),new Vector2(.96f,.92f));
            var title=MenuText("Title",plaque.transform,"CURSED DECK",70,new Vector2(.14f,.27f),new Vector2(.86f,.73f));
            title.fontStyle=FontStyles.Bold;title.characterSpacing=2;title.enableVertexGradient=true;title.color=Color.white;
            title.colorGradient=new VertexGradient(new Color(1,.898f,.498f),new Color(1,.898f,.498f),new Color(.702f,.561f,0),new Color(.702f,.561f,0));
            MenuText("Eyebrow",startMenu,"THE HAUNTED CASINO",23,new Vector2(.10f,.735f),new Vector2(.90f,.77f)).characterSpacing=4;
            for(int i=0;i<3;i++)
            {
                var card=UIPackLayout.Icon("Card-"+i,startMenu,skin!=null?skin.CardBack:back,Vector2.zero,Vector2.one);
                UiFactory.Place((RectTransform)card.transform,.5f+(i-1)*.14f,.623f,180,252);
                card.transform.localRotation=Quaternion.Euler(0,0,(1-i)*14);
            }
            MenuText("Tagline",startMenu,"One more hand. One more curse.",27,new Vector2(.12f,.51f),new Vector2(.88f,.55f)).color=UiFactory.Cream;
            menuContinue=MenuButton("ContinueRun",startMenu,"CONTINUE",.48f,.06f);
            menuNew=MenuButton("NewRun",startMenu,"NEW RUN",.408f,.06f);
            menuSettings=MenuButton("Settings",startMenu,"SETTINGS",.192f,.06f);
            menuStats=MenuButton("Stats",startMenu,"STATS & UNLOCKS",.12f,.06f);
            menuDaily=MenuButton("DailyChallenge",startMenu,"DAILY CHALLENGE",.336f,.06f);
            menuTutorial=MenuButton("ReplayTutorial",startMenu,"REPLAY TUTORIAL",.264f,.06f);
            checkpointLabel=MenuText("Checkpoint",startMenu,"A new hand awaits.",22,new Vector2(.10f,.067f),new Vector2(.90f,.087f));checkpointLabel.color=UiFactory.Cream;
            MenuText("Footer",startMenu,"TRIPEAKS SOLITAIRE  /  ROGUELIKE",18,new Vector2(.08f,.035f),new Vector2(.92f,.065f)).characterSpacing=2;
            startMenu.gameObject.SetActive(false);

            pauseMenu=Panel("PauseMenu");
            var frame=UIPackLayout.Panel("Frame",pauseMenu,skin!=null?skin.ResourcePanel:null,new Vector2(.06f,.205f),new Vector2(.94f,.80f));
            MenuSurface(frame.transform);
            var inner=UiFactory.Rect("Content",frame.transform);UiFactory.Anchor(inner,new Vector2(.09f,.075f),new Vector2(.91f,.925f));
            MenuText("Eyebrow",inner,"THE HOUSE CAN WAIT",20,new Vector2(.03f,.88f),new Vector2(.97f,.94f)).characterSpacing=3;
            MenuText("Title",inner,"PAUSED",62,new Vector2(.03f,.72f),new Vector2(.97f,.88f)).fontStyle=FontStyles.Bold;
            pauseDetail=MenuText("Checkpoint",inner,"",25,new Vector2(.03f,.61f),new Vector2(.97f,.72f));pauseDetail.color=UiFactory.Cream;
            pauseResume=MenuButton("Resume",inner,"RESUME",.49f,.135f);
            pauseSettings=MenuButton("Settings",inner,"SETTINGS",.31f,.135f);
            pauseTitle=MenuButton("MainMenu",inner,"SAVE & MAIN MENU",.13f,.135f);
            pauseMenu.gameObject.SetActive(false);

            newRunConfirm=Panel("NewRunConfirmation");
            var warning=UIPackLayout.Panel("Frame",newRunConfirm,skin!=null?skin.ResourcePanel:null,new Vector2(.06f,.28f),new Vector2(.94f,.72f));
            MenuSurface(warning.transform);
            MenuText("Title",warning.transform,"START A NEW RUN?",44,new Vector2(.08f,.72f),new Vector2(.92f,.88f));
            var detail=MenuText("Detail",warning.transform,"This replaces your unfinished run.\nYour unlocks and settings are kept.",29,new Vector2(.12f,.45f),new Vector2(.88f,.69f));
            detail.textWrappingMode=TextWrappingModes.Normal;detail.color=UiFactory.Cream;
            confirmNew=MenuButton("Confirm",warning.transform,"START NEW RUN",.32f,.15f);
            cancelNew=MenuButton("Cancel",warning.transform,"KEEP MY RUN",.13f,.15f);
            newRunConfirm.gameObject.SetActive(false);
        }
        private TextMeshProUGUI MenuText(string name,Transform parent,string text,float size,Vector2 min,Vector2 max)
        {var label=UiFactory.Text(name,parent,font,text,size,UiFactory.Gold);UiFactory.Anchor(label.rectTransform,min,max);UIPackLayout.Fit(label,16);return label;}
        private static void MenuSurface(Transform frame)
        {
            // The supplied small panel has a textured center; retain its border without magnifying that texture.
            var surface=UIPackLayout.Panel("InnerSurface",frame,null,new Vector2(.018f,.018f),new Vector2(.982f,.982f));
            surface.color=new Color(.012f,.018f,.015f,.985f);surface.transform.SetAsFirstSibling();
        }
        private UnityEngine.UI.Button MenuButton(string name,Transform parent,string text,float y,float height=.07f)
        {
            var button=UiFactory.Button(name,parent,font,text,34,front,UiFactory.Green);
            UiFactory.Anchor((RectTransform)button.transform,new Vector2(.12f,y-height*.5f),new Vector2(.88f,y+height*.5f));
            if(skin!=null)UIPackLayout.Button(button,skin.SecondaryButton);
            var label=button.GetComponentInChildren<TextMeshProUGUI>();label.color=UiFactory.Gold;label.margin=new Vector4(42,12,42,12);UIPackLayout.Fit(label,18);
            return button;
        }
        private void BindMenus()
        {
            menuContinue.onClick.AddListener(ResumeMenu);
            menuNew.onClick.AddListener(RequestMenuRun);menuDaily.onClick.AddListener(RequestDailyRun);
            menuSettings.onClick.AddListener(OpenSettings);menuTutorial.onClick.AddListener(ReplayTutorial);
            menuStats.onClick.AddListener(OpenStats);
            pauseResume.onClick.AddListener(ResumeMenu);
            pauseSettings.onClick.AddListener(OpenSettings);
            pauseTitle.onClick.AddListener(ReturnToTitle);
            confirmNew.onClick.AddListener(StartMenuRun);
            cancelNew.onClick.AddListener(CancelMenuRun);
        }
        private void ReplayTutorial(){settingsPanel.gameObject.SetActive(false);metaPanel.gameObject.SetActive(false);queuedTutorial=false;tutorialSuspended=false;bus.Send(new GameCommand(GameCommandKind.StartTutorial));}
        private void OpenStats(){metaPanel.gameObject.SetActive(true);metaPanel.SetAsLastSibling();bus.Send(new GameCommand(GameCommandKind.MetaRefresh));}
        private void ResumeMenu()=>bus.Send(new GameCommand(GameCommandKind.Resume));
        public void OpenPause()=>bus.Send(new GameCommand(GameCommandKind.Pause));
        private void ReturnToTitle()=>bus.Send(new GameCommand(GameCommandKind.ReturnToTitle));
        private void RequestMenuRun()
        {
            pendingDaily=false;
            if(previous==null || previous.Menu!=MenuMode.Title)return;
            if(previous.CanContinueRun){newRunConfirm.gameObject.SetActive(true);newRunConfirm.SetAsLastSibling();}
            else StartMenuRun();
        }
        private void RequestDailyRun(){pendingDaily=true;if(previous==null||previous.Menu!=MenuMode.Title)return;if(previous.CanContinueRun){newRunConfirm.gameObject.SetActive(true);newRunConfirm.SetAsLastSibling();}else StartMenuRun();}
        private void StartMenuRun(){newRunConfirm.gameObject.SetActive(false);bus.Send(new GameCommand(pendingDaily?GameCommandKind.MenuDaily:GameCommandKind.MenuNewRun));pendingDaily=false;}
        private void CancelMenuRun()=>newRunConfirm.gameObject.SetActive(false);
        private void RenderMenus(GameSnapshot state)
        {
            bool title=state.Menu==MenuMode.Title,paused=state.Menu==MenuMode.Paused;
            startMenu.gameObject.SetActive(title);pauseMenu.gameObject.SetActive(paused);
            if(title)
            {
                menuContinue.interactable=state.CanContinueRun;
                checkpointLabel.text=!string.IsNullOrEmpty(state.Notice)?state.Notice:state.CanContinueRun?"FLOOR "+state.Floor+" / ROUND "+state.Round+"  -  CONTINUE YOUR HAND":"A new hand awaits.";
                startMenu.SetAsLastSibling();
            }
            if(paused){pauseDetail.text="FLOOR "+state.Floor+" / ROUND "+state.Round+"\nSCORE "+state.Score.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);pauseMenu.SetAsLastSibling();}
            if(title||paused)
            {
                if(tutorial.gameObject.activeSelf){tutorialSuspended=true;tutorial.gameObject.SetActive(false);}
                if(settingsPanel.gameObject.activeSelf)settingsPanel.SetAsLastSibling();
                if(metaPanel.gameObject.activeSelf)metaPanel.SetAsLastSibling();
                if(newRunConfirm.gameObject.activeSelf)newRunConfirm.SetAsLastSibling();
            }
            else
            {
                newRunConfirm.gameObject.SetActive(false);
                if(queuedTutorial){queuedTutorial=false;tutorialSuspended=false;ShowTutorial();}
                else if(tutorialSuspended){tutorialSuspended=false;UpdateTutorialFocus(state);}
            }
            // Stats remain available from a menu, but starting another mode there would bypass confirmation.
            if(RecoveryVisible)recoveryPanel.SetAsLastSibling();
            foreach(string mode in MenuDeckButtons){var button=metaPanel.Find(mode).GetComponent<UnityEngine.UI.Button>();button.interactable=state.Menu==MenuMode.Playing && (mode!="RED DECK"||redUnlocked);}
        }
        private static readonly string[] MenuDeckButtons={"CLASSIC","RED DECK","DAILY"};
        public void NavigateBack()
        {
            if(!initialized || previous==null)return;
            if(RecoveryVisible){DismissRecovery();return;}
            if(CloseCardInspection())return;
            if(CloseOwnedModal())return;
            if(newRunConfirm.gameObject.activeSelf){CancelMenuRun();return;}
            if(settingsPanel.gameObject.activeSelf){CloseSettings();return;}
            if(metaPanel.gameObject.activeSelf){metaPanel.gameObject.SetActive(false);return;}
            if(previous.Menu==MenuMode.Paused)ResumeMenu();else if(previous.Menu==MenuMode.Playing)OpenPause();
        }
    }
}
