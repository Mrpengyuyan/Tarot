Shader "TarotUnity/ScryingOrb"
{
    // A crystal ball, which is almost entirely a shading problem: the geometry is
    // just a sphere. Three things make glass read as glass, and the orb had none
    // of them - it was a matte sphere with a flat tint, i.e. a plastic marble.
    //
    //  1. Fresnel. Reflectance climbs toward grazing angles, so a glass sphere
    //     has a bright rim and a dark centre. This is the single biggest tell.
    //  2. Something inside. The interior is sampled through a parallax offset
    //     along the view direction, so it sits behind the surface instead of
    //     being painted on it, and slides as the camera breathes.
    //  3. A hard specular. One small, tight highlight per light - glass does not
    //     have the broad soft falloff of the wax next to it.
    Properties
    {
        _InteriorTex ("Interior (equirect)", 2D) = "black" {}
        _InteriorColor ("Interior Tint", Color) = (0.55, 0.5, 1, 1)
        _InteriorDepth ("Interior Depth", Range(0, 0.5)) = 0.22
        _GlassColor ("Glass Tint", Color) = (0.16, 0.14, 0.3, 1)
        _RimColor ("Rim Color", Color) = (0.72, 0.7, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.2
        _RimIntensity ("Rim Intensity", Range(0, 4)) = 1.5
        _SpecPower ("Specular Tightness", Range(8, 256)) = 96
        _SpecIntensity ("Specular Intensity", Range(0, 8)) = 2.6
        _Opacity ("Opacity", Range(0, 1)) = 0.94
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 normalOS : TEXCOORD2;
                float3 viewOS : TEXCOORD3;
            };

            TEXTURE2D(_InteriorTex);
            SAMPLER(sampler_InteriorTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _InteriorTex_ST;
                float4 _InteriorColor;
                float _InteriorDepth;
                float4 _GlassColor;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _SpecPower;
                float _SpecIntensity;
                float _Opacity;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalOS = normalize(IN.normalOS);

                // Object-space view direction: lets the interior parallax stay put
                // relative to the orb rather than swimming with world rotation.
                float3 camOS = TransformWorldToObject(GetCameraPositionWS());
                OUT.viewOS = normalize(camOS - IN.positionOS.xyz);
                return OUT;
            }

            // Equirectangular lookup for a direction.
            float2 DirToEquirect(float3 dir)
            {
                float u = atan2(dir.z, dir.x) * (1.0 / (2.0 * PI)) + 0.5;
                float v = asin(clamp(dir.y, -1.0, 1.0)) * (1.0 / PI) + 0.5;
                return float2(u, v);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetCameraPositionWS() - IN.positionWS);

                // Push the sample direction into the ball along the view ray. The
                // offset is what sells "behind glass"; without it the nebula is a
                // decal on the surface.
                float3 interiorDir = normalize(IN.normalOS - IN.viewOS * _InteriorDepth * 2.0);
                float2 uv = DirToEquirect(interiorDir);
                half3 interior = SAMPLE_TEXTURE2D(_InteriorTex, sampler_InteriorTex, uv).rgb;
                interior *= _InteriorColor.rgb;

                // Fresnel: dark through the middle, bright at the edge.
                float fres = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);

                // The interior is most visible looking straight in, and is
                // swallowed by the rim at grazing angles - same as real glass.
                half3 colour = lerp(interior, _GlassColor.rgb, fres * 0.65);
                colour += _RimColor.rgb * fres * _RimIntensity;

                // Tight speculars from the candles. Glass answers a light with a
                // small hard dot, not the broad falloff of the wax beside it.
                Light mainLight = GetMainLight();
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), _SpecPower);
                colour += mainLight.color * spec * _SpecIntensity;

                #ifdef _ADDITIONAL_LIGHTS
                uint count = GetAdditionalLightsCount();
                for (uint i = 0u; i < count; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    float3 hi = normalize(l.direction + V);
                    float si = pow(saturate(dot(N, hi)), _SpecPower);
                    colour += l.color * si * _SpecIntensity * l.distanceAttenuation;
                }
                #endif

                // Edge-on glass is denser than glass seen face-on.
                float alpha = saturate(_Opacity + fres * 0.4);
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
