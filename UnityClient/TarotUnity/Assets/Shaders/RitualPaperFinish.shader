Shader "Tarot/RitualPaperFinish"
{
    Properties
    {
        _Stroke ("Linear Stroke Ink", Vector) = (1,1,1,1)
        _ShadowOnly ("Shadow Only", Float) = 0
        _Style ("Border Offset Sigma Opacity", Vector) = (1,12,8,0.6)
        _Viewport ("Camera Pixel Rectangle", Vector) = (0,0,1280,720)
        _Corner0 ("Top Left", Vector) = (0,0,0,0)
        _Corner1 ("Top Right", Vector) = (0,0,0,0)
        _Corner2 ("Bottom Right", Vector) = (0,0,0,0)
        _Corner3 ("Bottom Left", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+1" }
        Pass
        {
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Stroke, _Style, _Viewport;
                float4 _Corner0, _Corner1, _Corner2, _Corner3;
                float _ShadowOnly;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; };
            struct Output { float4 positionCS : SV_POSITION; float4 screen : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screen = ComputeScreenPos(o.positionCS);
                return o;
            }
            float Cross(float2 a, float2 b) { return a.x*b.y-a.y*b.x; }
            float Segment(float2 p, float2 a, float2 b)
            {
                float2 edge = b-a;
                return length(p-a-edge*saturate(dot(p-a,edge)/max(dot(edge,edge),0.00001)));
            }
            float Distance(float2 p)
            {
                float2 a=_Corner0.xy,b=_Corner1.xy,c=_Corner2.xy,d=_Corner3.xy;
                float4 sides=float4(Cross(b-a,p-a),Cross(c-b,p-b),Cross(d-c,p-c),Cross(a-d,p-d));
                bool inside=min(min(sides.x,sides.y),min(sides.z,sides.w))>=0
                    || max(max(sides.x,sides.y),max(sides.z,sides.w))<=0;
                float distance=min(min(Segment(p,a,b),Segment(p,b,c)),min(Segment(p,c,d),Segment(p,d,a)));
                return inside ? -distance : distance;
            }
            float GaussianTail(float x)
            {
                float t=1/(1+0.2316419*abs(x));
                float p=0.39894228*exp(-0.5*x*x)*t*(0.31938153+t*(-0.35656378+t*(1.78147794+t*(-1.82125598+t*1.33027443))));
                return x>=0 ? p : 1-p;
            }
            float LinearShadowAlpha(float alpha)
            {
                // Match a CSS black shadow's perceived darkness in Unity's linear blend target.
                float value=1-saturate(alpha);
                float luminance=value<=0.04045 ? value/12.92 : pow((value+0.055)/1.055,2.4);
                return 1-luminance;
            }
            half4 frag(Output i) : SV_Target
            {
                float2 p=i.screen.xy/i.screen.w*_Viewport.zw+_Viewport.xy;
                float sd=Distance(p);
                float footprint=max(1.0,fwidth(sd));
                float coverage=saturate(0.5-sd/footprint);
                if (_ShadowOnly>0.5)
                {
                    float shadow=GaussianTail(Distance(p+float2(0,_Style.y))/max(_Style.z,0.001))*_Style.w*(1-coverage);
                    shadow=LinearShadowAlpha(shadow);
                    clip(shadow-0.0001);
                    return half4(0,0,0,shadow);
                }
                float stroke=coverage-saturate(0.5-(sd+_Style.x)/footprint);
                clip(stroke*_Stroke.w-0.0001);
                return half4(_Stroke.rgb,stroke*_Stroke.w);
            }
            ENDHLSL
        }
    }
}
