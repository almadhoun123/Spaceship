// Simulated cloud shadows: a thin shell just above the planet surface that darkens (multiplies)
// whatever is under it, using the same cloud shape as the cloud layer (via CloudShadowSync globals).
// Drawn after the planet and before the clouds, so it darkens the ground and sea, never the clouds.
Shader "Spaceships/CloudShadowLayer"
{
    Properties
    {
        _Strength ("Shadow Strength", Range(0, 1)) = 0.65
        _ShadowColor ("Shadow Color (what the ground is multiplied by in full shadow)", Color) = (0.12, 0.14, 0.22, 1)
        _Softness ("Softness", Range(0, 10)) = 2
        _Offset ("Offset (0 = directly under the cloud, 1 = physical)", Range(0, 1)) = 0.1
        _HighLayerShadow ("High Layer Shadow", Range(0, 1)) = 0.6
        _DayFade ("Fade Toward Night (shadows only where sunlit)", Range(0.01, 1)) = 0.25
        _FollowOpacity ("Follow Cloud Opacity (0 = shadow from cloud shape only)", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-30" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend DstColor Zero         // multiply: darkens what's behind
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CloudNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Strength;
                half4 _ShadowColor;
                half _Softness;
                half _Offset;
                half _HighLayerShadow;
                half _DayFade;
                half _FollowOpacity;
            CBUFFER_END

            // Cloud settings shared by CloudShadowSync on the Clouds layer.
            float _CG_Coverage, _CG_CoverageScale, _CG_CoverageSoftness;
            float _CG_DetailScale, _CG_Warp, _CG_Puffiness, _CG_PuffScale, _CG_Threshold, _CG_EdgeSoftness;
            float _CG_Speed, _CG_Evolve, _CG_Opacity, _CG_ShellScale;
            float _CG_HighScale, _CG_HighStretch, _CG_HighThreshold, _CG_HighSpeed, _CG_HighOpacity, _CG_HighParallax;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.positionOS);
                float3 s = normalize(TransformWorldToObjectDir(GetMainLight().direction));

                // Where the cloud above this point is: straight up, leaning toward the Sun by Offset.
                float b = dot(d, s);
                float t = -b + sqrt(max(b * b - (1 - _CG_ShellScale * _CG_ShellScale), 0));
                CloudParams c;
                c.coverage = _CG_Coverage; c.coverageScale = _CG_CoverageScale; c.coverageSoftness = _CG_CoverageSoftness;
                c.detailScale = _CG_DetailScale; c.warp = _CG_Warp; c.puffiness = _CG_Puffiness; c.puffScale = _CG_PuffScale;
                c.threshold = _CG_Threshold; c.edgeSoftness = _CG_EdgeSoftness; c.speed = _CG_Speed; c.evolve = _CG_Evolve;
                c.highScale = _CG_HighScale; c.highStretch = _CG_HighStretch; c.highThreshold = _CG_HighThreshold; c.highSpeed = _CG_HighSpeed;

                float footprint = length(fwidth(d)) + _Softness * 0.004;
                float puff;
                // Shadow follows cloud density; how much it also follows the clouds' opacity is adjustable,
                // so thin-looking clouds can still cast a clear shadow.
                float low = CloudLowDensity(normalize(d + s * t * _Offset), _Time.y, 4, footprint, c, puff)
                            * lerp(1, _CG_Opacity, _FollowOpacity);
                float high = CloudHighDensity(normalize(d + s * (t + _CG_HighParallax) * _Offset), _Time.y, footprint, c)
                             * lerp(1, _CG_HighOpacity, _FollowOpacity) * _HighLayerShadow;
                float shadow = 1 - (1 - low) * (1 - high);

                // Only where the Sun is up (no point darkening the night side further).
                shadow *= smoothstep(-0.05, _DayFade, dot(d, s)) * _Strength;
                return half4(lerp(float3(1, 1, 1), _ShadowColor.rgb, shadow), 1);
            }
            ENDHLSL
        }
    }
}
