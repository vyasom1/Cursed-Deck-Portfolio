using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
namespace CursedDeck.ViewTests
{
    public sealed class DirectDailyMenuTests
    {
        [Test] public void DailyReplacementRequiresConfirmationAndCanBeCancelled()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();var root=new GameObject("DailyMenu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameView));
            try
            {
                var view=root.GetComponent<GameView>();view.Initialize(bus,content.Font,content.CardFront,content.CardBack,content.SuitIcons);
                using(var host=new GameHost(new RunController(42,content.CreateCatalog()),bus,content.CreatePresentation(),new UiSettings()))
                {
                    int starts=0;host.MenuDailyRequested+=()=>starts++;host.ShowStartMenu(true);view.StartDailyChallengeButton.onClick.Invoke();Assert.That(starts,Is.Zero);
                    var confirm=view.SafeRoot.Find("NewRunConfirmation");Assert.That(confirm.gameObject.activeSelf);var buttons=confirm.GetComponentsInChildren<Button>(true);Assert.That(buttons.Length,Is.EqualTo(2));
                    foreach(var b in buttons)if(b.name=="Cancel")b.onClick.Invoke();Assert.That(starts,Is.Zero);Assert.That(host.Menu,Is.EqualTo(MenuMode.Title));
                    view.StartDailyChallengeButton.onClick.Invoke();foreach(var b in buttons)if(b.name=="Confirm")b.onClick.Invoke();Assert.That(starts,Is.EqualTo(1));Assert.That(host.Menu,Is.EqualTo(MenuMode.Playing));
                    bus.Send(new GameCommand(GameCommandKind.MenuDaily));Assert.That(starts,Is.EqualTo(1));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
