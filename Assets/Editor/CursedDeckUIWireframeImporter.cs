using UnityEditor;
namespace CursedDeck.EditorTools
{
    public static class CursedDeckUIWireframeImporter
    {
        [MenuItem("Tools/Cursed Deck/Wire UI Pack to Prefabs")]
        public static void Wire()=>CursedDeck.Bootstrap.Editor.UIWireframePipeline.Prepare();
    }
}
