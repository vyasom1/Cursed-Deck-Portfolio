using System.Collections.Generic;
using UnityEngine;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private void InjectRelicIcons()
        {
            var icons=new Dictionary<string,Sprite>(System.StringComparer.Ordinal);
            foreach(var relic in content.Relics)icons[relic.CreateSpec().Id]=relic.Icon;
            foreach(var relic in metaContent.UnlockRelics)icons[relic.CreateSpec().Id]=relic.Icon;
            View.SetRelicIcons(icons);
        }
    }
}
