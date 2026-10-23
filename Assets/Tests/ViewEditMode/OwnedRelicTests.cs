using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
namespace CursedDeck.ViewTests
{
    public sealed class OwnedRelicTests
    {
        [Test] public void TwentyDefinitionsHaveIconsAndOwnedBadgeOpensRealBuffDetails()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var meta=Resources.Load<MetaDefinition>("CursedDeck/Meta");var definitions=content.Relics.Concat(meta.UnlockRelics).ToArray();Assert.That(content.Relics.Count,Is.EqualTo(15));Assert.That(definitions.Length,Is.EqualTo(20));Assert.That(definitions.Select(r=>r.CreateSpec().Id).Distinct().Count(),Is.EqualTo(20));foreach(var relic in definitions)Assert.That(relic.Icon,Is.Not.Null);
            var catalog=content.CreateCatalog();var run=new RunController(42,catalog,startingRelics:new[]{catalog[0]});var bus=new EventBus();var root=new GameObject("OwnedRelicsTest",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameView));
            try
            {
                var view=root.GetComponent<GameView>();view.Initialize(bus,content.Font,content.CardFront,content.CardBack,content.SuitIcons);view.SetRelicIcons(definitions.ToDictionary(r=>r.CreateSpec().Id,r=>r.Icon));using(var host=new GameHost(run,bus,content.CreatePresentation(),new UiSettings()))
                {host.Publish(GameFeedback.Deal);string before=run.ToJson();view.OwnedRelicsButton.onClick.Invoke();Assert.That(view.OwnedRelicsVisible);view.OwnedRelicButton(0).onClick.Invoke();Assert.That(view.RelicDetailVisible);Assert.That(view.SafeRoot.Find("RelicDetail/Icon").GetComponent<Image>().sprite,Is.Not.Null);Assert.That(view.SafeRoot.Find("RelicDetail/ActiveBuff").GetComponent<TextMeshProUGUI>().text,Is.Not.Empty);view.NavigateBack();Assert.That(view.OwnedRelicsVisible);view.NavigateBack();Assert.That(view.OwnedRelicsVisible,Is.False);Assert.That(run.ToJson(),Is.EqualTo(before));}
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
