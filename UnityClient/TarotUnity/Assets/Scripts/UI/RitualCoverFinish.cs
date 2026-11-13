using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1150)]
    public sealed class RitualCoverFinish : MonoBehaviour
    {
        private TMP_Text title;
        private Image shade;
        private Material original, ink;
        private Texture2D gradient;
        private bool vertexGradient;
        private Color originalColor;
        private static readonly float[] Stops = { .08f,.32f,.56f,.74f,1f };
        private static readonly Color[] Gold = { new(1,240f/255,207f/255),new(226f/255,198f/255,141f/255),
            new(183f/255,139f/255,76f/255),new(244f/255,219f/255,168f/255),new(197f/255,157f/255,96f/255) };

        internal static void Attach(GameObject host,TMP_Text title)
        {
            if (host==null || title==null) return;
            var owner=host.GetComponent<RitualCoverFinish>() ?? host.AddComponent<RitualCoverFinish>();
            owner.title=title;
            owner.Build();
        }

        private void OnEnable() => Build();

        private void Build()
        {
            if (!isActiveAndEnabled || title==null || ink!=null) return;
            var shadeMaterial=Resources.Load<Material>("Presentation/RitualCoverShade");
            var titleMaterial=Resources.Load<Material>("Presentation/RitualTitleInk");
            if (shadeMaterial==null || titleMaterial==null) { Debug.LogError("Ritual cover finishing material is missing."); return; }
            original=title.fontSharedMaterial;
            vertexGradient=title.enableVertexGradient;
            originalColor=title.color;
            gradient=new Texture2D(512,1,TextureFormat.RGBA32,false)
            { name="RitualTitleGold",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear };
            var colors=new Color[512];
            for(var i=0;i<colors.Length;i++)
            {
                var t=i/(float)(colors.Length-1);
                var band=0;
                while(band<3 && t>Stops[band+1]) band++;
                colors[i]=Color.Lerp(Gold[band],Gold[band+1],Mathf.InverseLerp(Stops[band],Stops[band+1],t));
            }
            gradient.SetPixels(colors);
            gradient.Apply(false,false);
            ink=new Material(original) { name="RitualTitleInk",hideFlags=HideFlags.HideAndDontSave,shader=titleMaterial.shader };
            ink.SetTexture("_FaceTex",gradient);
            ink.SetTextureScale("_FaceTex",Vector2.one);
            ink.SetTextureOffset("_FaceTex",Vector2.zero);
            title.OnPreRenderText+=MapTitleInk;
            var node=new GameObject("RitualCoverShade",typeof(RectTransform),typeof(Image));
            node.transform.SetParent(transform,false);
            node.transform.SetAsFirstSibling();
            var rect=(RectTransform)node.transform;
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            shade=node.GetComponent<Image>();
            shade.material=shadeMaterial;
            shade.raycastTarget=false;
            RestoreInk();
        }

        private void LateUpdate() => RestoreInk();

        private void RestoreInk()
        {
            if (title==null || ink==null) return;
            if (title.fontSharedMaterial!=ink) title.fontSharedMaterial=ink;
            if (title.enableVertexGradient) title.enableVertexGradient=false;
            if (title.color!=Color.white) title.color=Color.white;
        }

        private void MapTitleInk(TMP_TextInfo info)
        {
            var rect=title.rectTransform.rect;
            var length=rect.width*.25881905f+rect.height*.96592583f;
            if (length<=.001f) return;
            // The final CSS gradient is165deg across the whole unrotated title box, not each glyph.
            for(var i=0;i<info.characterCount;i++)
            {
                var glyph=info.characterInfo[i];
                if(!glyph.isVisible) continue;
                var mesh=info.meshInfo[glyph.materialReferenceIndex];
                for(var j=0;j<4;j++)
                {
                    var vertex=glyph.vertexIndex+j;
                    var point=(Vector2)mesh.vertices[vertex]-rect.center;
                    mesh.uvs2[vertex]=new Vector2(.5f+(point.x*.25881905f-point.y*.96592583f)/length,.5f);
                }
            }
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private void Release()
        {
            if(title!=null)
            {
                title.OnPreRenderText-=MapTitleInk;
                if(ink!=null && title.fontSharedMaterial==ink)
                {
                    title.fontSharedMaterial=original;
                    title.enableVertexGradient=vertexGradient;
                    title.color=originalColor;
                    title.havePropertiesChanged=true;
                }
            }
            if(shade!=null) { shade.gameObject.SetActive(false); Destroy(shade.gameObject); }
            if(ink!=null) Destroy(ink);
            if(gradient!=null) Destroy(gradient);
            shade=null; ink=null; gradient=null;
        }
    }
}
