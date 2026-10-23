using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>One cached quad; board-only contrast, without texture creation or polling allocations.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardArtShade : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin,r.yMin),new Color(0,0,0,.20f),Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin,r.yMax),new Color(0,0,0,.74f),Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax,r.yMax),new Color(0,0,0,.74f),Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax,r.yMin),new Color(0,0,0,.20f),Vector2.zero);
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
        }
    }
}
