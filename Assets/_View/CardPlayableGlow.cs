using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>Feathered light outside the single ornate frame. Geometry is static; only renderer tint pulses.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardPlayableGlow : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;float w=9;
            Edge(mesh,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(-w,0));
            Edge(mesh,new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),new Vector2(w,0));
            Edge(mesh,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),new Vector2(0,w));
            Edge(mesh,new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMin),new Vector2(0,-w));
        }
        private static void Edge(UnityEngine.UI.VertexHelper mesh,Vector2 a,Vector2 b,Vector2 offset)
        {
            int n=mesh.currentVertCount;mesh.AddVert(a,Color.white,Vector2.zero);mesh.AddVert(b,Color.white,Vector2.zero);
            mesh.AddVert(b+offset,Color.clear,Vector2.zero);mesh.AddVert(a+offset,Color.clear,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n+2,n+3,n);
        }
        public void Pulse(float age)
        {
            float t=.5f+.5f*Mathf.Sin(age*2.8f);var tint=Color.Lerp(new Color(1,.77f,.20f),new Color(0,1,.53f),t);tint.a=.55f+.4f*t;
            canvasRenderer.SetColor(tint);
        }
    }
}
