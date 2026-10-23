using UnityEngine;
using UnityEngine.UI;
namespace CursedDeck.View
{
    /// <summary>Fixed ambient UI particle pool. Its visual PRNG never consumes the run RNG.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AmbientCastleMist:MaskableGraphic
    {
        private struct Mote { public float X,Y,Speed,Radius,Phase; }
        private readonly Mote[] motes=new Mote[64];
        private uint random=0xC045EDu;private bool ready;private float age;
        public int Capacity=>motes.Length;
        private float Next(){random=1664525u*random+1013904223u;return (random>>8)*(1f/16777216f);}
        public void Initialize()
        {
            if(ready)return;ready=true;raycastTarget=false;
            material=Resources.Load<Material>("TCG/CardSpecularGlow");
            for(int i=0;i<motes.Length;i++)motes[i]=new Mote{X=Next(),Y=Next(),Speed=.007f+Next()*.013f,Radius=i<48?60+Next()*140:2+Next()*3,Phase=Next()*Mathf.PI*2};
        }
        protected override void OnEnable(){base.OnEnable();Initialize();}
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void Step(float delta)
        {
            if(!isActiveAndEnabled)return;Initialize();age+=Mathf.Max(0,delta);
            for(int i=0;i<motes.Length;i++){var m=motes[i];m.X=Mathf.Repeat(m.X+m.Speed*delta,1);m.Y=Mathf.Repeat(m.Y+m.Speed*delta*.23f,1);motes[i]=m;}
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!ready)return;var r=rectTransform.rect;var vertex=UIVertex.simpleVert;
            for(int i=0;i<motes.Length;i++)
            {
                var m=motes[i];var center=new Vector3(r.xMin+m.X*r.width,r.yMin+m.Y*r.height+Mathf.Sin(age*.12f+m.Phase)*18);
                float alpha=(i<48?.02f:.23f)*(.7f+.3f*Mathf.Sin(age*.6f+m.Phase));int sides=i<48?24:8;int first=vh.currentVertCount;
                vertex.position=center;vertex.color=new Color(.02f,.9f,.43f,alpha);vh.AddVert(vertex);
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertex.position=center+new Vector3(Mathf.Cos(a)*m.Radius,Mathf.Sin(a)*m.Radius*(i<48?.38f:1));vertex.color=new Color(0,.75f,.3f,0);vh.AddVert(vertex);}
                for(int j=0;j<sides;j++)vh.AddTriangle(first,first+1+j,first+1+(j+1)%sides);
            }
        }
    }
}
