// Procedural planet clouds on their own shell (a sphere slightly larger than the planet).
//  - Shape: a low-frequency "weather" noise decides where the big cloud systems and the big clear
//    areas are; inside them a domain-warped (swirled) detail noise makes the billows and streaks.
//  - Layers: a thick low layer that shades itself away from the Sun (depth), and a thin, faster,
//    stretched high layer of wisps on top.
//  - Light: not self-lit. White in daylight, a warm band along the day/night line, dark and mostly
//    transparent at night.
//  - Motion: both layers drift around the planet's axis at their own speeds and slowly change shape.
// The noise is 3D on the sphere's surface, so there is no texture, seam or pole pinching.
Shader "Spaceships/Clouds"
{
    Properties
    {
        [Header(Cloud systems big clear and cloudy areas)]
        _Coverage ("Coverage (how much of the planet is cloudy)", Range(0, 1)) = 0.42
        _CoverageScale ("System Size (lower = bigger systems)", Range(0.5, 8)) = 2.2
        _CoverageSoftness ("System Edge Softness", Range(0.01, 0.5)) = 0.09

        [Header(Cloud detail)]
        _DetailScale ("Detail Size (lower = bigger billows)", Range(2, 60)) = 22
        _Warp ("Swirl (domain warp)", Range(0, 4)) = 0.35
        _Puffiness ("Puffiness (billowy puffs vs smooth sheets)", Range(0, 1)) = 0.35
        _PuffScale ("Puff Size (relative to detail)", Range(1, 6)) = 3
        _Threshold ("Density Threshold", Range(0, 1)) = 0.42
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.24
        _InnerVariation ("Inner Variation (thin and thick patches)", Range(0, 1)) = 0.5
        _Opacity ("Opacity", Range(0, 1)) = 0.95

        [Header(Depth and shading)]
        _SelfShadow ("Self Shadow Strength", Range(0, 8)) = 1.6
        _ShadowOffset ("Self Shadow Distance", Range(0, 0.05)) = 0.007
        _ShadowBrightness ("Shadowed Side Brightness", Range(0, 1)) = 0.62

        [Header(High wispy layer)]
        _HighOpacity ("High Layer Opacity", Range(0, 1)) = 0.25
        _HighScale ("High Layer Size", Range(2, 60)) = 12
        _HighStretch ("High Layer Streakiness", Range(1, 6)) = 2
        _HighThreshold ("High Layer Threshold", Range(0, 1)) = 0.55
        _HighParallax ("High Layer Height (parallax)", Range(0, 0.03)) = 0.008

        [Header(Motion)]
        _Speed ("Drift Speed", Range(-0.05, 0.05)) = 0.004
        _HighSpeed ("High Layer Drift Speed", Range(-0.05, 0.05)) = 0.007
        _Evolve ("Shape Change Speed", Range(0, 0.2)) = 0.02

        [Header(Light)]
        _CloudColor ("Cloud Color (lit)", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 4)) = 1.1
        _Ambient ("Ambient (everywhere)", Range(0, 0.5)) = 0.01
        _NightOpacity ("Opacity on the Night Side", Range(0, 1)) = 0.85
        _NightColor ("Night Cloud Color", Color) = (0.55, 0.62, 0.8, 1)
        _NightBrightness ("Night Cloud Brightness", Range(0, 1)) = 0.12
        _SunsetWidth ("Day/Night Softness", Range(0.01, 1)) = 0.3
        _DiffuseFalloff ("Sun Angle Falloff (0 = evenly bright, 1 = dims as the Sun gets low)", Range(0, 1)) = 0

        [Header(Day and night line tint)]
        _TerminatorColor ("Color (day side of the band)", Color) = (1.0, 0.62, 0.32, 1)
        _TerminatorColor2 ("Color (night side of the band)", Color) = (0.85, 0.32, 0.18, 1)
        _TerminatorTint ("Amount", Range(0, 1)) = 0.75
        _TerminatorOffset ("Position (- toward night, + toward day)", Range(-0.5, 0.5)) = 0.02
        _TerminatorWidth ("Width", Range(0.01, 1)) = 0.35
        _TerminatorSharpness ("Sharpness", Range(0.25, 8)) = 1.2
        _TerminatorBrightness ("Band Glow (1 = no extra light)", Range(0, 6)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-20" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha      // premultiplied
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Coverage, _CoverageScale, _CoverageSoftness;
                half _DetailScale, _Warp, _Puffiness, _PuffScale, _Threshold, _EdgeSoftness, _InnerVariation, _Opacity;
                half _SelfShadow, _ShadowOffset, _ShadowBrightness;
                half _HighOpacity, _HighScale, _HighStretch, _HighThreshold, _HighParallax;
                half _Speed, _HighSpeed, _Evolve;
                half4 _CloudColor; half _Brightness, _Ambient, _NightOpacity, _SunsetWidth;
                half4 _NightColor; half _NightBrightness;
                half4 _TerminatorColor, _TerminatorColor2;
                half _TerminatorTint, _TerminatorOffset, _TerminatorWidth, _TerminatorSharpness;
                half _DiffuseFalloff, _TerminatorBrightness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            #include "CloudNoise.hlsl"
            #define Fbm CloudFbm
            #define RotateY CloudRotateY

            CloudParams MaterialCloudParams()
            {
                CloudParams c;
                c.coverage = _Coverage; c.coverageScale = _CoverageScale; c.coverageSoftness = _CoverageSoftness;
                c.detailScale = _DetailScale; c.warp = _Warp; c.puffiness = _Puffiness; c.puffScale = _PuffScale;
                c.threshold = _Threshold; c.edgeSoftness = _EdgeSoftness; c.speed = _Speed; c.evolve = _Evolve;
                c.highScale = _HighScale; c.highStretch = _HighStretch; c.highThreshold = _HighThreshold; c.highSpeed = _HighSpeed;
                return c;
            }

            float LowDensity(float3 dir, float t, int detailOctaves, float footprint, out float puff)
            {
                return CloudLowDensity(dir, t, detailOctaves, footprint, MaterialCloudParams(), puff);
            }

            // High, thin, streaky layer (stretched along the drift direction).
            float HighDensity(float3 dir, float t, float footprint)
            {
                return CloudHighDensity(dir, t, footprint, MaterialCloudParams());
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 dir = normalize(i.positionOS);
                float t = _Time.y;
                float footprint = length(fwidth(dir));

                Light sun = GetMainLight();
                float3 sunOS = normalize(TransformWorldToObjectDir(sun.direction));
                float3 viewOS = normalize(TransformWorldToObjectDir(GetWorldSpaceNormalizeViewDir(i.positionWS)));

                // Sunlight on this part of the planet, and the warm band along the day/night line.
                float sunHeight = dot(dir, sunOS);
                float day = smoothstep(-_SunsetWidth * 0.4, _SunsetWidth, sunHeight);
                float bandX = (sunHeight - _TerminatorOffset) / _TerminatorWidth;
                float band = pow(saturate(1 - abs(bandX)), _TerminatorSharpness) * _TerminatorTint;
                float3 bandColor = lerp(_TerminatorColor2.rgb, _TerminatorColor.rgb, saturate(bandX * 0.5 + 0.5));
                float3 lightColor = lerp(_CloudColor.rgb, bandColor, band) * sun.color * _Brightness;
                float visible = lerp(_NightOpacity, 1, day);

                // How much sunlight reaches the clouds here: dims with a low Sun (falloff), and the
                // day/night band can glow brighter than plain daylight.
                float sunAngle = saturate((sunHeight + _SunsetWidth * 0.4) / (1 + _SunsetWidth * 0.4));
                float dayLight = day * lerp(1, sqrt(sunAngle), _DiffuseFalloff) * (1 + band * (_TerminatorBrightness - 1));

                // Low layer with self shadow: sample again a little toward the Sun.
                float puff, puffShadow;
                float low = LowDensity(dir, t, 6, footprint, puff);
                float3 toSun = normalize(sunOS - dir * sunHeight + 1e-5);       // along the surface, toward the Sun
                float lowShadow = LowDensity(normalize(dir + toSun * _ShadowOffset), t, 3, footprint, puffShadow);
                float shade = lerp(_ShadowBrightness, 1, exp(-lowShadow * _SelfShadow));
                // Thin and thick patches inside the clouds: thinner = more see-through and a bit darker.
                float thickness = lerp(1 - _InnerVariation, 1, smoothstep(0.3, 0.7, puff));
                shade *= lerp(1 - _InnerVariation * 0.35, 1, thickness);
                low *= thickness;
                float3 nightLight = _NightColor.rgb * _NightBrightness * (1 - day);   // dim, cool night clouds
                float3 lowCol = (lightColor * shade * dayLight + nightLight * shade + _Ambient) * low;
                float lowA = low * _Opacity * visible;

                // High layer, shifted by parallax so it floats above the low one.
                float3 viewTangent = viewOS - dir * dot(viewOS, dir);
                float high = HighDensity(normalize(dir - viewTangent * _HighParallax), t, footprint);
                float3 highCol = (lightColor * dayLight + nightLight + _Ambient) * high;
                float highA = high * _HighOpacity * visible;

                // High over low (premultiplied).
                float3 col = highCol * _HighOpacity * visible + lowCol * _Opacity * visible * (1 - highA);
                float alpha = highA + lowA * (1 - highA);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
