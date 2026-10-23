using NUnit.Framework;
using UnityEngine;
using CursedDeck.Data;
namespace CursedDeck.ViewTests
{
    public sealed class ReadableCardTests
    {
        [Test] public void EveryCharacterUsesASeparateFrameFreeDisplayPortrait()
        {
            var db=Resources.Load<GameContent>("CursedDeck/Content").Characters;
            foreach(var c in db.Characters)
            {
                Assert.That(c.DisplayPortrait,Is.Not.Null);
                if(c.SourceArt==null){Assert.That(c.DisplayPortrait,Is.SameAs(c.FallbackArt));continue;}
                Assert.That(c.DisplayPortrait,Is.Not.SameAs(c.SourceArt));Assert.That(c.DisplayPortrait.rect.width,Is.LessThan(c.SourceArt.rect.width));
            }
        }
        [Test] public void ProtectedRankAndSuitBadgesStayInsideSmallCardBounds()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var root=Object.Instantiate(Resources.Load<CursedDeck.View.CardView>("TCG/CardView")).gameObject;
            try
            {
                var card=root.GetComponent<CursedDeck.View.CardView>();card.Initialize(null,new CursedDeck.Run.EventBus(),data.Font,data.CardFront,data.CardBack,data.SuitIcons);card.Bind(new CursedDeck.Run.CardUiState(0,0,0,10,2,true,true,true,false),false);card.SetLayout(100,140);
                var rank=root.transform.Find("CardMotionRoot/CardFront/HeaderBanner/RankBadge/RankNumber").GetComponent<TMPro.TextMeshProUGUI>();Assert.That(rank.fontSize,Is.GreaterThanOrEqualTo(28));
                foreach(string path in new[]{"CardMotionRoot/CardFront/HeaderBanner/RankBadge","CardMotionRoot/CardFront/SuitBadgeTopRight","CardMotionRoot/CardFront/SuitBadgeBottomLeft"})
                {
                    var rect=(RectTransform)root.transform.Find(path);var corners=new Vector3[4];rect.GetWorldCorners(corners);
                    foreach(var corner in corners){var local=card.Rect.InverseTransformPoint(corner);Assert.That(local.x,Is.InRange(card.Rect.rect.xMin-.5f,card.Rect.rect.xMax+.5f),path);Assert.That(local.y,Is.InRange(card.Rect.rect.yMin-.5f,card.Rect.rect.yMax+.5f),path);}
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
