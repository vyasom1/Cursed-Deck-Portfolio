using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using CursedDeck.Bootstrap.Editor;
using CursedDeck.Data;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.ViewTests
{
    public sealed class CardArtPipelineTests
    {
        [Test] public void MatrixContainsEveryExistingSuitRankAnd52UniqueNames()
        {
            var db=CardArtPipeline.EnsureDatabase();db.Validate();Assert.That(db.Characters.Count,Is.EqualTo(52));
            Assert.That(db.Characters.Select(c=>c.CharacterName).Distinct().Count(),Is.EqualTo(52));
            for(int suit=0;suit<4;suit++)for(int rank=1;rank<=13;rank++){var c=db.Find(suit,rank);Assert.That(c,Is.Not.Null);Assert.That(c.Portrait,Is.Not.Null);Assert.That(c.FallbackArt,Is.Not.Null);Assert.That(c.Emblem,Is.Not.Null);}
            Assert.That(db.Find(3,1).FactionTitle,Is.EqualTo("Shadow Order"));Assert.That(db.Find(2,1).FactionTitle,Is.EqualTo("Blood Covenant"));Assert.That(db.Find(1,1).FactionTitle,Is.EqualTo("Frost Constellation"));Assert.That(db.Find(0,1).FactionTitle,Is.EqualTo("Void Conclave"));
        }
        [Test] public void ExportHas52CopyReadyPromptsAndExactFileNames()
        {
            string text=CardArtPipeline.Prompts(CardArtPipeline.EnsureDatabase());var prompts=text.Split('\n').Where(l=>l.StartsWith("Dark fantasy card art portrait, ")).ToArray();Assert.That(prompts.Length,Is.EqualTo(52));Assert.That(prompts.Distinct().Count(),Is.EqualTo(52));
            foreach(string line in prompts){Assert.That(line.Trim(),Does.EndWith("vertical 3:4 aspect ratio, no text."));Assert.That(line,Does.Contain("dark atmospheric background, painterly TCG card style"));}
            Assert.That(text,Does.Contain("[Spades_1.png]"));Assert.That(text,Does.Contain("[Hearts_13.png]"));string path=Path.Combine(Path.GetTempPath(),"CursedDeck-Prompts-"+Guid.NewGuid()+".txt");
            try{CardArtPipeline.ExportPrompts(path);Assert.That(File.ReadAllText(path),Is.EqualTo(text));}
            finally{if(File.Exists(path))File.Delete(path);}
        }
        [TestCase("Spades_1.png",true,3,1)] [TestCase("Hearts_13.png",true,2,13)] [TestCase("Diamonds_11.png",true,1,11)] [TestCase("Clubs_2.png",true,0,2)]
        [TestCase("spades_1.png",false,0,0)] [TestCase("Spades_0.png",false,0,0)] [TestCase("Hearts_14.png",false,0,0)] [TestCase("Spades_01.png",false,0,0)] [TestCase("Spades_K.png",false,0,0)] [TestCase("Spades_1.jpg",false,0,0)]
        public void CanonicalFileNamesAreValidated(string file,bool expected,int suit,int rank){bool result=CardArtPipeline.TryParseFileName(file,out int actualSuit,out int actualRank);Assert.That(result,Is.EqualTo(expected));if(expected){Assert.That(actualSuit,Is.EqualTo(suit));Assert.That(actualRank,Is.EqualTo(rank));}}
        [Test] public void SourceReplacementRemovalAndUndoKeepNamesAndFallbacks()
        {
            string folder="Assets/TCG-ImporterTest-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);AssetDatabase.Refresh();var db=UnityEngine.Object.Instantiate(CardArtPipeline.EnsureDatabase());
            try
            {
                string path=folder+"/Hearts_13.png";var texture=new Texture2D(12,16);texture.SetPixels(Enumerable.Repeat(Color.magenta,192).ToArray());texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());File.WriteAllBytes(folder+"/spades_1.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                string name=db.Find(2,13).CharacterName;var previousSource=db.Find(2,13).SourceArt;CardArtPipeline.BindArt(db,folder,CardArtPipeline.FallbackFolder);var entry=db.Find(2,13);var source=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(db.Find(3,1).SourceArt,Is.Null,"Non-canonical filename case must not bind, even on Windows.");Assert.That(entry.SourceArt,Is.SameAs(source));Assert.That(entry.Portrait,Is.SameAs(source));Assert.That(entry.CharacterName,Is.EqualTo(name));
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));Assert.That(importer.maxTextureSize,Is.EqualTo(512));Assert.That(importer.isReadable,Is.False);Assert.That(importer.GetPlatformTextureSettings("Android").maxTextureSize,Is.EqualTo(512));
                Undo.FlushUndoRecordObjects();Undo.PerformUndo();
                if(previousSource==null){Assert.That(db.Find(2,13).SourceArt==null,Is.True);Assert.That(db.Find(2,13).Portrait,Is.SameAs(db.Find(2,13).FallbackArt));}
                else Assert.That(db.Find(2,13).SourceArt,Is.SameAs(previousSource));
                Assert.That(db.Find(2,13).Portrait,Is.Not.Null);
                CardArtPipeline.BindArt(db,folder,CardArtPipeline.FallbackFolder);AssetDatabase.DeleteAsset(path);Assert.That(db.Find(2,13).Portrait,Is.SameAs(db.Find(2,13).FallbackArt));
                CardArtPipeline.BindArt(db,folder,CardArtPipeline.FallbackFolder);Assert.That(db.Find(2,13).SourceArt,Is.Null);Assert.That(db.Characters.All(c=>c.Portrait!=null),Is.True);Assert.That(db.Find(2,13).CharacterName,Is.EqualTo(name));
            }
            finally{UnityEngine.Object.DestroyImmediate(db);AssetDatabase.DeleteAsset(folder);}
        }
        [Test] public void AllFallbackTexturesHaveDistinctContentAndSafeImportSettings()
        {
            var hashes=new System.Collections.Generic.HashSet<string>();using(var sha=System.Security.Cryptography.SHA256.Create())foreach(var c in CardArtPipeline.EnsureDatabase().Characters)hashes.Add(Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(c.FallbackArt)))));Assert.That(hashes.Count,Is.EqualTo(52));
            foreach(var c in CardArtPipeline.EnsureDatabase().Characters){var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(c.FallbackArt));Assert.That(importer.maxTextureSize,Is.EqualTo(512));Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));}
        }
        [Test] public void CatalogRejectsDuplicateIdentityOrMissingArt()
        {
            var db=ScriptableObject.CreateInstance<CardCharacterDatabase>();try{var cards=CardCharacterMatrix.Create();cards[0]=cards[1];Assert.Throws<InvalidOperationException>(()=>db.SetCharacters(cards));db.SetCharacters(CardCharacterMatrix.Create());Assert.Throws<InvalidOperationException>(()=>db.Validate());}finally{UnityEngine.Object.DestroyImmediate(db);}
        }
    }
}
