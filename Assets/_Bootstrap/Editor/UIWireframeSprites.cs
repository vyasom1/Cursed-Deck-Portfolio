using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class UIWireframeSprites
    {
        public static bool Trimmed(string path)=>path.Contains("/Panels/")||path.Contains("/Buttons/")||path.Contains("/Icons/")||path.EndsWith("/TitlePlaque.png");
        public static Rect Trim(TextureImporter importer,bool panel,Rect bounds)
        {
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Sprite editor provider unavailable: "+importer.assetPath);
            provider.InitSpriteEditorDataProvider();var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(edit==null)throw new InvalidOperationException("Sprite edit capability unavailable: "+importer.assetPath);
            var capability=edit.GetEditCapability();
            foreach(var required in new[]{EEditCapability.EditSpriteRect,EEditCapability.EditBorder,EEditCapability.EditPivot,EEditCapability.EditSpriteName,EEditCapability.CreateAndDeleteSprite})
                if(!capability.HasCapability(required))throw new InvalidOperationException("Unsupported sprite operation "+required+": "+importer.assetPath);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();if(names==null)throw new InvalidOperationException("Sprite name/ID provider unavailable.");
            var rects=provider.GetSpriteRects();
            // One PNG is one supplied UI role; consolidate Unity-generated disconnected alpha fragments.
            SpriteRect rect=new SpriteRect{spriteID=GUID.Generate()};float largest=-1;foreach(var candidate in rects){float area=candidate.rect.width*candidate.rect.height;if(area>largest){largest=area;rect=candidate;}}
            string name=Path.GetFileNameWithoutExtension(importer.assetPath);
            var border=panel?new Vector4(Mathf.Min(160,bounds.width*.22f),Mathf.Min(96,bounds.height*.28f),Mathf.Min(160,bounds.width*.22f),Mathf.Min(96,bounds.height*.28f)):Vector4.zero;
            bool changed=rects.Length!=1||rect.rect!=bounds||rect.border!=border||rect.name!=name||rect.alignment!=SpriteAlignment.Center||rect.pivot!=new Vector2(.5f,.5f);
            if(changed)
            {
                rect.name=name;rect.rect=bounds;rect.border=border;rect.alignment=SpriteAlignment.Center;rect.pivot=new Vector2(.5f,.5f);
                provider.SetSpriteRects(new[]{rect});names.SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(name,rect.spriteID)});provider.Apply();importer.SaveAndReimport();
            }
            return bounds;
        }
        public static Rect AlphaBounds(string path)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false))throw new InvalidOperationException("Cannot decode PNG: "+path);
                var pixels=texture.GetPixels32();int x0=texture.width,y0=texture.height,x1=-1,y1=-1;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>4){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
                if(x1<0)throw new InvalidOperationException("UI sprite is fully transparent: "+path);
                // Preserve a four-pixel transparent safety rim. Source PNGs are never rewritten.
                x0=Mathf.Max(0,x0-4);y0=Mathf.Max(0,y0-4);x1=Mathf.Min(texture.width,x1+5);y1=Mathf.Min(texture.height,y1+5);
                return new Rect(x0,y0,x1-x0,y1-y0);
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
