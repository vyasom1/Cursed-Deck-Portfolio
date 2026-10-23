using UnityEditor;
namespace CursedDeck.EditorTools
{
    public static class CardArtImporter
    {
        [MenuItem("Tools/Cursed Deck/Full Setup & Asset Import")]
        public static void FullSetup() => CursedDeck.Bootstrap.Editor.CardArtPipeline.FullSetup();
    }
}
