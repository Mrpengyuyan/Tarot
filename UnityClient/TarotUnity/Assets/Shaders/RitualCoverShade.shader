Shader "Tarot/RitualCoverShade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 position:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float opacity:COLOR; };
            Output vert(Input v)
            {
                Output o;
                o.position=UnityObjectToClipPos(v.position);
                o.uv=v.uv;
                o.opacity=v.color.a;
                return o;
            }
            half4 frag(Output i):SV_Target
            {
                float y=1-i.uv.y;
                float alpha=y<.45 ? lerp(144.0/255,16.0/255,saturate(y/.45))
                    : lerp(16.0/255,153.0/255,saturate((y-.65)/.35));
                float transmittance=1-alpha;
                float opacity=1-(transmittance<=.04045 ? transmittance/12.92 : pow((transmittance+.055)/1.055,2.4));
                return half4(float3(3,3,2)*(alpha/(255.0*12.92)),opacity)*i.opacity;
            }
            ENDHLSL
        }
    }
}
