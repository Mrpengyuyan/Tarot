using System;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TarotUnity.Presentation
{
    internal sealed class RitualDeckShadow : IDisposable
    {
        internal MeshRenderer Renderer { get; }
        private readonly Mesh mesh;
        private readonly Vector3[] vertices=new Vector3[4];
        private readonly Vector2[] uv=new Vector2[4];
        private readonly MaterialPropertyBlock ink=new();

        internal static RitualDeckShadow Attach(Transform parent)
        {
            var material=Resources.Load<Material>("Presentation/RitualDeckShadow");
            if(material==null) { Debug.LogError("Ritual deck shadow material is missing."); return null; }
            return new RitualDeckShadow(parent,material);
        }

        private RitualDeckShadow(Transform parent,Material material)
        {
            var node=new GameObject("RitualDeckShadow",typeof(MeshFilter),typeof(MeshRenderer));
            node.transform.SetParent(parent,false);
            mesh=new Mesh { name="RitualDeckShadowQuad",hideFlags=HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            mesh.vertices=vertices;
            mesh.triangles=new[] { 0,1,2,0,2,3 };
            node.GetComponent<MeshFilter>().sharedMesh=mesh;
            Renderer=node.GetComponent<MeshRenderer>();
            Renderer.sharedMaterial=material;
            Renderer.sortingOrder=-200;
            Renderer.shadowCastingMode=ShadowCastingMode.Off;
            Renderer.receiveShadows=false;
            Renderer.lightProbeUsage=LightProbeUsage.Off;
            Renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            RitualPaperRendering.Register(Renderer);
        }

        internal void Refresh(RitualDeckPose pose,Vector3 frame,Quaternion rotation,float fov,int width,int height,float visible,float opacity)
        {
            if(Renderer==null) return;
            Renderer.enabled=visible>0f;
            if(!Renderer.enabled) return;
            var d=pose.Density;
            var min=new Vector2(-.05f*pose.Size.x-27*d,12*d-.02f*pose.Size.y-27*d);
            var max=new Vector2(1.05f*pose.Size.x+27*d,12*d+1.02f*pose.Size.y+27*d);
            var unit=2f*Mathf.Tan(fov*Mathf.Deg2Rad*.5f)/height;
            var layer=new RitualDeckLayerPose(new Vector3(0,0,-12*d));
            for(var i=0;i<4;i++)
            {
                uv[i]=new Vector2(i==0 || i==3 ? min.x : max.x,i<2 ? min.y : max.y);
                var point=RitualDeckLayout.ProjectPoint(pose,layer,new Vector2(uv[i].x/pose.Size.x,uv[i].y/pose.Size.y));
                var depth=2.5f*(1f-point.z/(900*d));
                var world=frame+rotation*new Vector3((point.x-width*.5f)*unit*depth,(height*.5f-point.y)*unit*depth,depth);
                vertices[i]=Renderer.transform.InverseTransformPoint(world);
            }
            mesh.vertices=vertices;
            mesh.uv=uv;
            mesh.RecalculateBounds();
            ink.SetVector("_Paper",new Vector4(pose.Size.x,pose.Size.y,d,pose.Opacity));
            ink.SetFloat("_ShadowOpacity",opacity*visible);
            Renderer.SetPropertyBlock(ink);
        }

        public void Dispose()
        {
            if(Renderer!=null)
            {
                Renderer.enabled=false;
                RitualPaperRendering.Unregister(Renderer);
                Renderer.gameObject.SetActive(false);
                Object.Destroy(Renderer.gameObject);
            }
            if(mesh!=null) Object.Destroy(mesh);
        }
    }
}
