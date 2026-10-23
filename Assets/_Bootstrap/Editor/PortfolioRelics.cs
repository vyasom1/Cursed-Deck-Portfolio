using System;
using System.IO;
using System.Linq;
using CursedDeck.Data;
using CursedDeck.Run;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PortfolioRelics
    {
        private readonly struct Definition
        {
            public readonly string Id,Name,Description;public readonly RelicKind Kind;public readonly int Amount,Interval;public readonly float Multiplier;
            public Definition(string id,string name,string description,RelicKind kind,int amount=1,int interval=1,float multiplier=1){Id=id;Name=name;Description=description;Kind=kind;Amount=amount;Interval=interval;Multiplier=multiplier;}
        }
        [MenuItem("Tools/Cursed Deck/Prepare Portfolio Relics")]
        public static void Prepare()
        {
            var definitions=new[]{
                new Definition("grave-compass","Grave Compass","Aces are wild while your chain is three or higher.",RelicKind.GraveCompass,interval:3),
                new Definition("void-step","Void Step","Every fourth board play may jump two ranks, including around K/A.",RelicKind.VoidStep,interval:4),
                new Definition("bone-abacus","Bone Abacus","Black cards gain 15 flat points before chain and relic multipliers.",RelicKind.BoneAbacus,amount:15),
                new Definition("soul-ledger","Soul Ledger","Earn 2 gold whenever you play a face card.",RelicKind.SoulLedger,amount:2),
                new Definition("still-hour","Still Hour","Start every round with two extra draw allowances. Creates no stock cards.",RelicKind.StillHour,amount:2),
                new Definition("frost-pocket","Frost Pocket","Your first normal draw each round refunds one allowance. Paid rerolls do not consume this effect.",RelicKind.FrostPocket),
                new Definition("bone-crown","Bone Crown","Aces score twice as much after the chain multiplier.",RelicKind.BoneCrown,multiplier:2)
            };
            foreach(var d in definitions)
            {
                string path="Assets/_Data/Relics/"+d.Id+".asset";var asset=AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);if(asset!=null)continue;
                asset=ScriptableObject.CreateInstance<RelicDefinition>();AssetDatabase.CreateAsset(asset,path);var s=new SerializedObject(asset);
                s.FindProperty("id").stringValue=d.Id;s.FindProperty("displayName").stringValue=d.Name;s.FindProperty("description").stringValue=d.Description;s.FindProperty("kind").intValue=(int)d.Kind;s.FindProperty("order").intValue=150+(int)d.Kind;s.FindProperty("amount").intValue=d.Amount;s.FindProperty("interval").intValue=d.Interval;s.FindProperty("multiplier").floatValue=d.Multiplier;s.ApplyModifiedPropertiesWithoutUndo();
            }
            // Reuse the existing included-in-build Style atlas scanner. No new delivery system or atlas script.
            var all=AssetDatabase.FindAssets("t:RelicDefinition",new[]{"Assets/_Data/Relics","Assets/_Data/UnlockRelics"}).Select(g=>AssetDatabase.LoadAssetAtPath<RelicDefinition>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(r=>r.CreateSpec().Id,StringComparer.Ordinal).ToArray();
            if(all.Length!=20)throw new InvalidOperationException("Expected 15 base plus five unlock definitions, found "+all.Length);
            for(int i=0;i<all.Length;i++)
            {
                var asset=all[i];if(asset.Icon!=null)continue;string path=CardArtPipeline.StyleFolder+"/Relic-"+asset.CreateSpec().Id+".png";
                if(!File.Exists(path))WriteRune(path,i);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var asset in all)
            {
                if(asset.Icon!=null)continue;var s=new SerializedObject(asset);s.FindProperty("relicIcon").objectReferenceValue=CardArtPipeline.ConfigureSprite(CardArtPipeline.StyleFolder+"/Relic-"+asset.CreateSpec().Id+".png");s.ApplyModifiedPropertiesWithoutUndo();
            }
            var content=Resources.Load<GameContent>("CursedDeck/Content");var so=new SerializedObject(content);var list=so.FindProperty("relics");var bases=all.Where(r=>AssetDatabase.GetAssetPath(r).StartsWith("Assets/_Data/Relics/",StringComparison.Ordinal)).ToArray();
            if(bases.Length!=15)throw new InvalidOperationException("Base relic pool must have 15 entries.");list.arraySize=bases.Length;for(int i=0;i<bases.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=bases[i];so.ApplyModifiedPropertiesWithoutUndo();
            var rules=new SerializedObject(content.Rules);rules.FindProperty("fallbackRewardGold").intValue=25;rules.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            CardArtPipeline.EnsureAtlases(content.Characters);Debug.Log("CURSED_DECK_PORTFOLIO_RELICS_READY base=15 unlock=5 icons=20");
        }
        private static void WriteRune(string path,int index)
        {
            const int size=128;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color32[size*size];Color tint=Color.HSVToRGB((index*.137f)%1,.68f,1);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+ .5f-size*.5f)/(size*.5f),v=(y+.5f-size*.5f)/(size*.5f),r=Mathf.Sqrt(u*u+v*v),a=Mathf.Atan2(v,u);
                float ring=Mathf.Exp(-Mathf.Pow((r-.74f)*50,2)),runeRadius=.28f+.10f*Mathf.Cos((3+index%6)*a+index*.31f);
                float rune=Mathf.Exp(-Mathf.Pow((r-runeRadius)*48,2)),spokes=Mathf.Abs(Mathf.Sin((2+index%5)*a))<.08f&&r>.42f&&r<.63f?1:0;
                float glow=Mathf.Exp(-Mathf.Pow((r-.74f)*13,2))*.27f;float strength=Mathf.Clamp01(ring+rune+spokes);Color c=Color.Lerp(new Color(.03f,.055f,.065f),tint,Mathf.Clamp01(strength+glow));c.a=r<.9f?1:Mathf.Clamp01((1-r)*10);pixels[y*size+x]=c;
            }
            try{texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
