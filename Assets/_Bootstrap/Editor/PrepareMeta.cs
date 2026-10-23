using CursedDeck.Data;
using CursedDeck.Run;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PrepareMeta
    {
        public static void Build()
        {
            string[] ids={"black-velvet","long-wick","low-stakes","high-stakes","first-light"};
            string[] descriptions={"Black cards score 50% more.","Chains of five or more score 50% more.","Ranks 2 to 6 score 50% more.","Face cards score 50% more.","The first three plays gain 25 flat points before multipliers."};
            var assets=new RelicDefinition[5];System.IO.Directory.CreateDirectory("Assets/_Data/UnlockRelics");AssetDatabase.Refresh();
            for(int i=0;i<5;i++)
            {
                string path="Assets/_Data/UnlockRelics/"+ids[i]+".asset";
                string previous="Assets/_Data/Relics/Unlocks/"+ids[i]+".asset";if(AssetDatabase.LoadAssetAtPath<RelicDefinition>(previous)!=null && AssetDatabase.LoadAssetAtPath<RelicDefinition>(path)==null)AssetDatabase.MoveAsset(previous,path);
                var asset=AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);if(asset==null){asset=ScriptableObject.CreateInstance<RelicDefinition>();AssetDatabase.CreateAsset(asset,path);}var s=new SerializedObject(asset);
                s.FindProperty("id").stringValue=ids[i];s.FindProperty("displayName").stringValue=ids[i].Replace('-',' ');s.FindProperty("description").stringValue=descriptions[i];s.FindProperty("kind").intValue=(int)RelicKind.BlackVelvet+i;s.FindProperty("order").intValue=200+i;s.FindProperty("amount").intValue=25;s.FindProperty("interval").intValue=i==4?3:5;s.FindProperty("multiplier").floatValue=1.5f;s.ApplyModifiedPropertiesWithoutUndo();assets[i]=asset;
            }
            string target="Assets/_Data/Resources/CursedDeck/Meta.asset";var meta=AssetDatabase.LoadAssetAtPath<MetaDefinition>(target);if(meta==null){meta=ScriptableObject.CreateInstance<MetaDefinition>();AssetDatabase.CreateAsset(meta,target);}var serialized=new SerializedObject(meta);var list=serialized.FindProperty("unlockRelics");list.arraySize=5;for(int i=0;i<5;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=assets[i];serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();Debug.Log("CURSED_DECK_META_READY");
        }
    }
}
