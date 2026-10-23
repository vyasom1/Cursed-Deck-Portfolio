using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using CursedDeck.Run;
using CursedDeck.Data;
using CursedDeck.View;

namespace CursedDeck.ViewTests
{
    public sealed class MenuViewTests
    {
        private GameObject root;private GameHost host;private GameView view;private EventBus bus;private RunController run;
        [TearDown]public void Cleanup(){host?.Dispose();if(root!=null)Object.DestroyImmediate(root);}
        private void Screen(int height=1920)
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");bus=new EventBus();run=new RunController(42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout());host=new GameHost(run,bus,content.CreatePresentation(),new UiSettings(true,false));
            root=Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;view=root.GetComponent<GameView>();view.Initialize(bus,content.Font,content.CardFront,content.CardBack,content.SuitIcons);
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;float scale=Mathf.Sqrt(height/1920f);((RectTransform)root.transform).sizeDelta=new Vector2(1080/scale,height/scale);
            view.SafeRoot.GetComponent<SafeAreaView>().Apply(new Rect(0,50,1080,height-100),1080,height);host.ShowStartMenu(false);Canvas.ForceUpdateCanvases();
        }
        private UnityEngine.UI.Button Button(string path)=>view.SafeRoot.Find(path).GetComponent<UnityEngine.UI.Button>();
        [Test]public void TitleContinuePauseSettingsAndConfirmationPreserveRunUntilExplicitReplacement()
        {
            Screen();Assert.That(view.StartMenuVisible);Assert.That(view.ResumeRunButton.interactable,Is.False);string before=run.ToJson();
            host.ShowStartMenu(true);view.StartNewRunButton.onClick.Invoke();Assert.That(view.SafeRoot.Find("NewRunConfirmation").gameObject.activeSelf);Assert.That(run.ToJson(),Is.EqualTo(before));
            Button("NewRunConfirmation/Frame/Cancel").onClick.Invoke();Assert.That(view.SafeRoot.Find("NewRunConfirmation").gameObject.activeSelf,Is.False);
            view.ResumeRunButton.onClick.Invoke();Assert.That(view.StartMenuVisible,Is.False);view.OpenPause();Assert.That(view.PauseMenuVisible);
            Button("PauseMenu/Frame/Content/Settings").onClick.Invoke();Assert.That(view.SafeRoot.Find("SettingsPanel").gameObject.activeSelf);view.CloseSettings();Assert.That(view.PauseMenuVisible);
            view.DrawButton.onClick.Invoke();Assert.That(run.ToJson(),Is.EqualTo(before));Button("PauseMenu/Frame/Content/Resume").onClick.Invoke();Assert.That(view.PauseMenuVisible,Is.False);
            view.OpenPause();Button("PauseMenu/Frame/Content/MainMenu").onClick.Invoke();Assert.That(view.StartMenuVisible);Assert.That(run.ToJson(),Is.EqualTo(before));
            int starts=0;host.MenuNewRunRequested+=()=>starts++;view.StartNewRunButton.onClick.Invoke();Button("NewRunConfirmation/Frame/Confirm").onClick.Invoke();Assert.That(starts,Is.EqualTo(1));Assert.That(view.StartMenuVisible,Is.False);
        }
        [TestCase(1920)][TestCase(2340)][TestCase(2400)]
        public void MenuActionsAndLabelsFitNotchedPortraitSizes(int height)
        {
            Screen(height);
            AssertLayout(view.SafeRoot.Find("StartMenu"));host.ShowStartMenu(true);view.ResumeRunButton.onClick.Invoke();view.OpenPause();AssertLayout(view.SafeRoot.Find("PauseMenu"));
            bus.Send(new GameCommand(GameCommandKind.ReturnToTitle));view.StartNewRunButton.onClick.Invoke();AssertLayout(view.SafeRoot.Find("NewRunConfirmation"));
        }
        private static void AssertLayout(Transform menu)
        {
            Canvas.ForceUpdateCanvases();var buttons=menu.GetComponentsInChildren<UnityEngine.UI.Button>();
            var bounds=Bounds((RectTransform)menu);
            foreach(var button in buttons){var transform=(RectTransform)button.transform;var rect=Bounds(transform);Assert.That(transform.rect.width,Is.GreaterThan(100));Assert.That(transform.rect.height,Is.GreaterThan(70));Assert.That(bounds.Contains(rect.min)&&bounds.Contains(rect.max),button.name);}
            for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)Assert.That(Bounds((RectTransform)buttons[i].transform).Overlaps(Bounds((RectTransform)buttons[j].transform)),Is.False,buttons[i].name+" overlaps "+buttons[j].name);
            foreach(var text in menu.GetComponentsInChildren<TextMeshProUGUI>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);}
        }
        private static Rect Bounds(RectTransform rect){var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c.Min(p=>p.x),c.Min(p=>p.y),c.Max(p=>p.x),c.Max(p=>p.y));}
    }
}
