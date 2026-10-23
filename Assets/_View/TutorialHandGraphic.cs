using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>Small procedural hand silhouette: no imported art, material instance or per-frame mesh generation.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialHandGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Rounded(vh,-15,-22,30,35,8);Rounded(vh,-15,4,9,38,4.5f);
            Rounded(vh,-5,0,9,22,4.5f);Rounded(vh,5,-3,9,20,4.5f);Rounded(vh,14,-8,8,18,4);
            Rounded(vh,-22,-13,12,22,6);Rounded(vh,-10,-32,23,15,3);
        }
        private void Rounded(UnityEngine.UI.VertexHelper vh,float x,float y,float w,float h,float radius)
        {
            Quad(vh,x,y+radius,w,h-2*radius);Quad(vh,x+radius,y,w-2*radius,h);
            Disk(vh,x+radius,y+radius,radius);Disk(vh,x+w-radius,y+radius,radius);
            Disk(vh,x+radius,y+h-radius,radius);Disk(vh,x+w-radius,y+h-radius,radius);
        }
        private void Quad(UnityEngine.UI.VertexHelper vh,float x,float y,float w,float h)
        {
            int start=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y),color,Vector2.zero);
            vh.AddVert(new Vector3(x+w,y+h),color,Vector2.zero);vh.AddVert(new Vector3(x,y+h),color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        private void Disk(UnityEngine.UI.VertexHelper vh,float x,float y,float radius)
        {
            int start=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);
            for(int i=0;i<=12;i++){float a=i*Mathf.PI/6;vh.AddVert(new Vector3(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),color,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}
        }
    }
}
