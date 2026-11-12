Shader "Tarot/RitualTableAtmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS : POSITION; };
            struct Output { float4 positionCS : SV_POSITION; float4 screen : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screen = ComputeScreenPos(o.positionCS);
                return o;
            }
            float NativeShadowAlpha(float alpha)
            {
                float value=1-alpha;
                return 1-(value<=0.04045 ? value/12.92 : pow((value+0.055)/1.055,2.4));
            }
            half4 frag(Output i) : SV_Target
            {
                float2 uv=i.screen.xy/i.screen.w;
                // CSS farthest-corner ellipse, with the vertical coordinate measured from the bottom.
                float radius=length((uv-float2(.5,.54))/(float2(.5,.54)*1.41421356));
                float alpha=radius<=.62 ? saturate((radius-.24)/.38)*(34.0/255)
                    : lerp(34.0/255,144.0/255,saturate((radius-.62)/.38));
                // Both premultiplied sRGB tints stay below .04045, so use the linear segment.
                float3 tint=float3(8,3,0)*(alpha/(255.0*12.92));
                alpha=NativeShadowAlpha(alpha);
                float warmRadius=length((uv-float2(.5,.475))/(float2(.175,.325)*1.41421356));
                float warm=(11.0/255)*saturate(1-warmRadius/.68);
                float3 warmInk=float3(229,169,66)*(warm/(255.0*12.92))*1.2;
                float3 ink=tint*(1-warm)+warmInk;
                float opacity=alpha*(1-warm)+warm;
                clip(opacity-.00001);
                return half4(ink,opacity);
            }
            ENDHLSL
        }
    }
}
