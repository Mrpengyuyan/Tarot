using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TarotUnity.Presentation
{
    /// <summary>Room-owned backdrop shading, before printed paper and outside the world grade.</summary>
    internal sealed class RitualTableAtmosphere : IDisposable
    {
        private readonly GameObject node;
        private readonly Mesh mesh;
        private readonly Vector3[] vertices = new Vector3[4];

        internal static RitualTableAtmosphere Attach(Camera camera,int layer)
        {
            var material = Resources.Load<Material>("Presentation/RitualTableAtmosphere");
            if (material == null)
            {
                Debug.LogError("Ritual table atmosphere material is missing.");
                return null;
            }
            return new RitualTableAtmosphere(camera,layer,material);
        }

        private RitualTableAtmosphere(Camera camera,int layer,Material material)
        {
            node = new GameObject("RitualTableAtmosphere",typeof(MeshFilter),typeof(MeshRenderer));
            node.transform.SetParent(camera.transform,false);
            node.layer = layer;
            mesh = new Mesh { name="RitualTableAtmosphereQuad",hideFlags=HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.triangles = new[] { 0,1,2,0,2,3 };
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = short.MinValue;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
            Fit(camera);
        }

        internal void Fit(Camera camera)
        {
            if (node == null || mesh == null || camera == null) return;
            var depth = Mathf.Lerp(camera.nearClipPlane,camera.farClipPlane,.01f);
            var halfHeight = camera.orthographic ? camera.orthographicSize
                : depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
            var halfWidth = halfHeight*camera.aspect;
            for (var i=0; i<4; i++) vertices[i] = new Vector3(i==0 || i==3 ? -halfWidth : halfWidth,
                i<2 ? halfHeight : -halfHeight,depth);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (node != null)
            {
                node.SetActive(false);
                Object.Destroy(node);
            }
            if (mesh != null) Object.Destroy(mesh);
        }
    }
}
