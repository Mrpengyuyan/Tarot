Shader "Hidden/Tarot/CaptureComposite"
{
    Properties { _MainTex ("UI", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            half4 frag(v2f_img input) : SV_Target { return tex2D(_MainTex, input.uv); }
            ENDHLSL
        }
    }
}
