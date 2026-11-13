Shader "Tarot/RitualDeckInk"
{
    Properties
    {
        [MainTexture] _MainTex ("Printed Back", 2D) = "white" {}
        _BorderColor ("Border", Color) = (0.7333333,0.5803922,0.3019608,1)
        _BorderSize ("Local Border", Vector) = (0.012,0.007,0,0)
        _Opacity ("Group Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _BorderColor;
                float4 _BorderSize;
                float _Opacity;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }
            float Coverage(float2 uv,float2 aa)
            {
                float2 edge=min(uv,1-uv);
                return min(saturate(edge.x/aa.x),saturate(edge.y/aa.y));
            }
            half4 frag(Output i) : SV_Target
            {
                float2 edge = min(i.uv, 1-i.uv);
                float2 aa = max(fwidth(i.uv), 0.00001);
                float2 border = 1-smoothstep(_BorderSize.xy-aa*0.5, _BorderSize.xy+aa*0.5, edge);
                float body=Coverage(i.uv,aa);
                float first=Coverage(i.uv+float2(0,_BorderSize.y),aa);
                float second=Coverage(i.uv+float2(0,2*_BorderSize.y),aa);
                float alpha = max(body,max(first,second))*_Opacity;
                clip(alpha-0.001);
                float2 uv = saturate((i.uv-_BorderSize.xy)/(1-2*_BorderSize.xy));
                half3 ink = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                ink=lerp(ink,_BorderColor.rgb,max(border.x,border.y));
                float3 stock=lerp(SRGBToLinear(float3(50,32,11)/255.0),SRGBToLinear(float3(169,135,73)/255.0),first);
                return half4(lerp(stock,ink,body),alpha);
            }
            ENDHLSL
        }
    }
}
