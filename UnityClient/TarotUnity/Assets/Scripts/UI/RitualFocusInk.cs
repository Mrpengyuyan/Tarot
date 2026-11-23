using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    public sealed class RitualFocusInk : Image
    {
        internal bool Halo;
        internal float Stroke=2f;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect=GetPixelAdjustedRect();
            if(Halo)
            {
                const int count=48;
                mesh.AddVert(rect.center,color,Vector2.zero);
                var edge=color; edge.a=0;
                for(var i=0;i<count;i++)
                {
                    var angle=i*2*Mathf.PI/count;
                    mesh.AddVert(rect.center+Vector2.Scale(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),rect.size*.4596194f),edge,Vector2.zero);
                }
                for(var i=0;i<count;i++) mesh.AddTriangle(0,i+1,(i+1)%count+1);
                return;
            }
            var width=Mathf.Min(Stroke,Mathf.Min(rect.width,rect.height)*.5f);
            Quad(rect.xMin,rect.yMin,rect.width,width);
            Quad(rect.xMin,rect.yMax-width,rect.width,width);
            Quad(rect.xMin,rect.yMin+width,width,rect.height-2*width);
            Quad(rect.xMax-width,rect.yMin+width,width,rect.height-2*width);

            void Quad(float x,float y,float w,float h)
            {
                var index=mesh.currentVertCount;
                mesh.AddVert(new Vector2(x,y),color,Vector2.zero);
                mesh.AddVert(new Vector2(x,y+h),color,Vector2.zero);
                mesh.AddVert(new Vector2(x+w,y+h),color,Vector2.zero);
                mesh.AddVert(new Vector2(x+w,y),color,Vector2.zero);
                mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
            }
        }
    }
}
