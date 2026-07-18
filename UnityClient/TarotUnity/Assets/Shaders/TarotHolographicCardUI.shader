Shader "TarotUnity/HolographicCardUI"
{
    // The UI counterpart of TarotUnity/HolographicCard. The flipped 3D card face
    // gets its foil from the real object-space view direction; this one lives on a
    // Screen-Space Overlay UI Image (the Result hero card), which has no view angle
    // to read - so the sheen direction is fed in through _Sheen and animated by
    // HolographicHeroCard (a slow idle drift, or the pointer position on hover).
    // The glare/iridescence maths is otherwise identical, so both cards shimmer as
    // one material language. Built on the standard UI shader template so it honours
    // the canvas clip rect and stencil (masks, scroll views).
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlareColor ("Glare Color", Color) = (1,1,1,1)
        _GlareIntensity ("Glare Intensity", Range(0,3)) = 0.7
        _GlareWidth ("Glare Width", Range(0.03,0.8)) = 0.22
        _GlareShift ("Glare Shift", Range(0,5)) = 2.2
        _Iridescence ("Iridescence", Range(0,1)) = 0.28
        _Sheen ("Sheen Dir (xy)", Vector) = (0,0,0,0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _GlareColor;
            float _GlareIntensity;
            float _GlareWidth;
            float _GlareShift;
            float _Iridescence;
            float4 _Sheen;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float3 Iris(float t)
            {
                return 0.5 + 0.5 * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // A diagonal band, positioned directly by _Sheen.x (the sweep, -1..1)
                // so the driver maps cleanly to a band that travels across the card;
                // _Sheen.y shifts the iridescent hue. The 3D face shader feeds these
                // from a real view direction, HolographicHeroCard from an idle drift
                // or the pointer.
                float coord = (IN.texcoord.x + IN.texcoord.y) * 0.5;
                float phase = 0.5 + _Sheen.x * _GlareShift * 0.5;
                float band = saturate(1.0 - abs(coord - phase) / max(_GlareWidth, 1e-3));
                band *= band;

                float3 glare = _GlareColor.rgb * band * _GlareIntensity;
                float3 irid = Iris(coord + _Sheen.y * 0.5) * band * _Iridescence;

                // Additive sheen, masked by the card's own alpha so it never lights
                // the transparent border.
                color.rgb += (glare + irid) * color.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
