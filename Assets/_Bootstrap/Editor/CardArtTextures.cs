using System;
using System.IO;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class CardArtTextures
    {
        public static Sprite EnsureEmblem(int suit)
        {
            string path=CardArtPipeline.StyleFolder+"/Emblem-"+CardCharacterMatrix.SuitNames[suit]+".png";
            Write(path,128,128,1,suit,1,Color.white);return CardArtPipeline.ConfigureSprite(path);
        }
        public static void EnsurePortrait(string path,int suit,int rank,Color color){Write(path,384,512,0,suit,rank,color);}
        public static Sprite Style(string name,int kind)
        {
            string path=CardArtPipeline.StyleFolder+"/"+name+".png";int w=kind==4||kind==5?64:300,h=kind==4||kind==5?64:420;
            Write(path,w,h,kind,0,1,Color.white);return CardArtPipeline.ConfigureSprite(path);
        }
        private static float Shape(int suit,float x,float y)
        {
            if(suit==1)return .56f-Mathf.Abs(x)-Mathf.Abs(y)*.76f;
            float stem=Mathf.Min(.10f-Mathf.Abs(x),Mathf.Min(y+.70f,-y));
            if(suit==0)return Mathf.Max(stem,Mathf.Max(.29f-Vector2.Distance(new Vector2(x,y),new Vector2(0,.32f)),Mathf.Max(.29f-Vector2.Distance(new Vector2(x,y),new Vector2(-.29f,-.06f)),.29f-Vector2.Distance(new Vector2(x,y),new Vector2(.29f,-.06f)))));
            float py=suit==3?-y:y;
            float lobes=Mathf.Max(.31f-Vector2.Distance(new Vector2(x,py),new Vector2(-.24f,.23f)),.31f-Vector2.Distance(new Vector2(x,py),new Vector2(.24f,.23f)));
            float tip=Mathf.Min((py+.66f)*.64f-Mathf.Abs(x),.23f-py);float heart=Mathf.Max(lobes,tip);
            return suit==3?Mathf.Max(heart,stem):heart;
        }
        private static void Write(string path,int w,int h,int kind,int suit,int rank,Color faction)
        {
            if(File.Exists(path))return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float u=(x+.5f)/w,v=(y+.5f)/h,px=(u-.5f)*2,py=(v-.5f)*2;Color c=Color.clear;
                if(kind==0)
                {
                    float d=Shape(suit,px*1.5f,(py-.05f)*1.5f);float radial=Mathf.Sqrt(px*px+py*py);
                    float angle=Mathf.Atan2(py,px),sigil=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle*(rank+3)+radial*12)),20)*Mathf.Exp(-Mathf.Abs(radial-.76f)*55);
                    uint hash=unchecked((uint)(x*374761393+y*668265263+rank*1234567+suit*98765));hash=(hash^(hash>>13))*1274126177;float grain=(hash&255)/255f;
                    float mist=(Mathf.Sin(px*8+py*13+rank)+Mathf.Sin(py*19-px*4+suit))*.5f;
                    c=Color.Lerp(new Color(.018f,.022f,.035f),faction,.08f+.12f*v+.025f*mist)+faction*(Mathf.Exp(-Mathf.Abs(d)*18)*.65f+sigil*.28f)+Color.white*(grain*.022f);
                    if(d>0)c=Color.Lerp(faction,Color.white,.28f+v*.25f);
                    c*=Mathf.Lerp(.65f,1,Mathf.Clamp01(1-radial*.35f));c.a=1;
                }
                else if(kind==1){float d=Shape(suit,px,py);c=Color.white;c.a=Mathf.Clamp01(d*40+.5f);}
                else if(kind==2)
                {
                    float edge=Mathf.Min(Mathf.Min(x,w-1-x),Mathf.Min(y,h-1-y));
                    bool border=edge<7||(edge>12&&edge<15);if(border)c=Color.Lerp(new Color(.34f,.22f,.10f),new Color(1,.86f,.51f),.45f+.5f*Mathf.Sin((u+v)*Mathf.PI));
                    foreach(var center in new[]{new Vector2(16,16),new Vector2(w-17,16),new Vector2(16,h-17),new Vector2(w-17,h-17)})
                    {float d=Vector2.Distance(new Vector2(x,y),center);if(d<15)c=d>10?new Color(.85f,.65f,.31f):new Color(.035f,.04f,.055f);}
                }
                else if(kind==3)
                {
                    float ax=Mathf.Max(0,Mathf.Abs(px)-.84f),ay=Mathf.Max(0,Mathf.Abs(py)-.88f);float d=Mathf.Sqrt(ax*ax+ay*ay);c=new Color(0,0,0,.6f*Mathf.Exp(-d*22));
                }
                else if(kind==4){float d=.62f-Mathf.Abs(px)-Mathf.Abs(py);c=Color.Lerp(new Color(.3f,.4f,.5f),Color.white,v);c.a=Mathf.Clamp01(d*20+.5f);}
                else if(kind==5){float d=.84f-Mathf.Max(Mathf.Abs(px)*.866f+Mathf.Abs(py)*.5f,Mathf.Abs(py));c=d<.12f?new Color(.85f,.70f,.38f):new Color(.055f,.06f,.08f);c.a=Mathf.Clamp01(d*35);}
                else if(kind==6){float band=Mathf.Exp(-Mathf.Pow((u+v-1)*3.6f,2));c=new Color(.8f,.9f,1,band*.7f);}
                pixels[y*w+x]=c;
            }
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
