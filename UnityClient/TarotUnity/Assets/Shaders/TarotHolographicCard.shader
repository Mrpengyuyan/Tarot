Shader "TarotUnity/HolographicCard"
{
    // Holographic foil sheen for the 3D card face (a SpriteRenderer). A diagonal
    // glare band plus a subtle iridescent tint ride over the card art, positioned
    // by the object-space view direction - so as the card tilts toward the
    // pointer (Phase 23) or the camera breathes (Phase 21), the sheen sweeps
    // across the face and the card reads as a glossy, dimensional object.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlareColor ("Glare Color", Color) = (1,1,1,1)
        _GlareIntensity ("Glare Intensity", Range(0,3)) = 1.05
        _GlareWidth ("Glare Width", Range(0.03,0.8)) = 0.24
        _GlareShift ("Glare View Shift", Range(0,5)) = 2.4
        _Iridescence ("Iridescence", Range(0,1)) = 0.32
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 viewOS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlareColor;
                float _GlareIntensity;
                float _GlareWidth;
                float _GlareShift;
                float _Iridescence;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                // Object-space view direction; its xy tilts away from zero as the
                // card turns relative to the camera, which moves the glare band.
                float3 camOS = TransformWorldToObject(GetCameraPositionWS());
                OUT.viewOS = normalize(camOS - IN.positionOS).xy;
                return OUT;
            }

            float3 Iris(float t)
            {
                return 0.5 + 0.5 * cos(6.2831853 * (t + float3(0.0, 0.33, 0.67)));
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float coord = (IN.uv.x + IN.uv.y) * 0.5;
                float phase = 0.5 + (IN.viewOS.x - IN.viewOS.y) * _GlareShift * 0.5;
                float band = saturate(1.0 - abs(coord - phase) / max(_GlareWidth, 1e-3));
                band *= band;

                float3 glare = _GlareColor.rgb * band * _GlareIntensity;
                float3 irid = Iris(coord + IN.viewOS.x * 0.5) * band * _Iridescence;

                // Additive sheen, masked by the card's own alpha so it only lights
                // the card, never the transparent border.
                art.rgb += (glare + irid) * art.a;
                return art;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
