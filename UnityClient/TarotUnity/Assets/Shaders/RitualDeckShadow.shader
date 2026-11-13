Shader "Tarot/RitualDeckShadow"
{
    Properties
    {
        _Paper ("Paper Width Height Density Opacity",Vector)=(100,200,1,1)
        _ShadowOpacity ("Shadow Opacity",Float)=.7
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-1" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Paper;
                float _ShadowOpacity;
            CBUFFER_END
            struct Input { float4 position:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.position=TransformObjectToHClip(v.position.xyz);
                o.uv=v.uv;
                return o;
            }
            float GaussianTail(float x)
            {
                float t=1/(1+.2316419*abs(x));
                float p=.39894228*exp(-.5*x*x)*t*(.31938153+t*(-.35656378+t*(1.78147794+t*(-1.82125598+t*1.33027443))));
                return x>=0 ? p : 1-p;
            }
            half4 frag(Output i):SV_Target
            {
                float2 halfSize=_Paper.xy*float2(.55,.52);
                float2 center=_Paper.xy*.5+float2(0,12*_Paper.z);
                float2 q=abs(i.uv-center)-halfSize;
                float distance=length(max(q,0))+min(max(q.x,q.y),0);
                float alpha=GaussianTail(distance/(9*_Paper.z))*(160.0/255)*_ShadowOpacity*_Paper.w;
                // A CSS opacity group composites its paper only once, not over a second shadow blend.
                if(_Paper.w<.99999 && i.uv.x>=0 && i.uv.x<=_Paper.x && i.uv.y>=0 && i.uv.y<=_Paper.y+2*_Paper.z) discard;
                float value=1-saturate(alpha);
                float opacity=1-(value<=.04045 ? value/12.92 : pow((value+.055)/1.055,2.4));
                clip(opacity-.0001);
                return half4(float3(5,1,0)*(alpha/(255.0*12.92)),opacity);
            }
            ENDHLSL
        }
    }
}
