// Earth: satellite color, shiny ocean / matte land (smoothness from the mask's alpha) and land relief
// from a normal map, lit with URP's own physically based lighting (same look as URP Lit).
//
// Why a custom shader: a normal map tilts mountain slopes toward the Sun even on the night side,
// so with URP Lit the dark half of the planet shows speckles of sunlit pixels. Here the normal map
// fades out as the Sun gets low (judged by the sphere's own surface, not the bumpy one), so the
// night side is never lit and the day/night line stays clean.
Shader "Spaceships/Earth"
{
    Properties
    {
        [MainTexture] _BaseMap ("Satellite Color", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)

        [Header(Ocean and land shine)]
        [NoScaleOffset] _MetallicGlossMap ("Smoothness Mask (A: ocean high, land low)", 2D) = "black" {}
        _Smoothness ("Smoothness Multiplier", Range(0, 1)) = 1

        [Header(Ocean roughness variation from the color map)]
        _OceanVariation ("Variation Amount (0 = uniform ocean)", Range(0, 1)) = 1
        _OceanSmoothDark ("Smoothness of Dark Water", Range(0, 1)) = 0.88
        _OceanSmoothBright ("Smoothness of Bright Water", Range(0, 1)) = 0.6
        _OceanDarkLevel ("Dark Water Brightness", Range(0, 1)) = 0.08
        _OceanBrightLevel ("Bright Water Brightness", Range(0, 1)) = 0.26
        _OceanVariationContrast ("Contrast", Range(0.25, 4)) = 1
        _OceanVariationBlur ("Softness (blurs the color map)", Range(0, 8)) = 2

        [Header(Land relief)]
        [Normal][NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 3)) = 0.7
        _NormalFade ("Normal Fade (Sun height where relief is full)", Range(0.01, 1)) = 0.25
        _MaxSlope ("Max Slope (caps the steepest cliffs)", Range(0.05, 3)) = 1
        _ReliefBlur ("Relief Blur (inland)", Range(0, 8)) = 1.5
        _BlurCompensation ("Keep Strength When Blurred", Range(0, 1)) = 0.15

        [Header(Relief near the coast)]
        _CoastBlur ("Coast Blur", Range(0, 8)) = 3
        _CoastStrength ("Coast Relief Strength (fraction of inland)", Range(0, 1)) = 0.5
        _CoastWidth ("Coast Width (how far inland the coast effect reaches)", Range(0, 10)) = 4
        _CoastFalloff ("Coast Falloff (higher = effect hugs the shore)", Range(0.25, 4)) = 1

        [Header(Night side)]
        _AmbientStrength ("Ambient Light", Range(0, 2)) = 1

        [Header(Cloud shadows  needs Cloud Shadow Sync on the Clouds layer)]
        [Toggle(_CLOUD_SHADOWS)] _CloudShadows ("Cloud Shadows", Float) = 0
        _CloudShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6
        _CloudShadowSoftness ("Shadow Softness", Range(0, 10)) = 2
        _CloudShadowOffset ("Shadow Offset (0 = directly under the cloud, 1 = physical)", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            half _OceanVariation;
            half _OceanSmoothDark;
            half _OceanSmoothBright;
            half _OceanDarkLevel;
            half _OceanBrightLevel;
            half _OceanVariationContrast;
            half _OceanVariationBlur;
            half _BumpScale;
            half _NormalFade;
            half _MaxSlope;
            half _ReliefBlur;
            half _BlurCompensation;
            half _CoastBlur;
            half _CoastStrength;
            half _CoastWidth;
            half _CoastFalloff;
            half _AmbientStrength;
            half _CloudShadowStrength;
            half _CloudShadowSoftness;
            half _CloudShadowOffset;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #pragma shader_feature_local _CLOUD_SHADOWS

            #if defined(_CLOUD_SHADOWS)
            #include "CloudNoise.hlsl"
            // Set every frame by CloudShadowSync on the Clouds layer, so shadows match the clouds.
            float _CG_Coverage, _CG_CoverageScale, _CG_CoverageSoftness;
            float _CG_DetailScale, _CG_Warp, _CG_Puffiness, _CG_PuffScale, _CG_Threshold, _CG_EdgeSoftness;
            float _CG_Speed, _CG_Evolve, _CG_Opacity, _CG_ShellScale;
            float _CG_HighScale, _CG_HighStretch, _CG_HighThreshold, _CG_HighSpeed, _CG_HighOpacity, _CG_HighParallax;

            // How much sunlight the clouds block at this point of the surface (0 = none).
            float CloudShadow(float3 positionWS, float3 sunDirWS)
            {
                float3 d = normalize(TransformWorldToObject(positionWS));
                float3 s = normalize(TransformWorldToObjectDir(sunDirWS));
                // Where the ray toward the Sun crosses the cloud shell (unit sphere -> shell radius).
                float b = dot(d, s);
                float t = -b + sqrt(max(b * b - (1 - _CG_ShellScale * _CG_ShellScale), 0));
                float3 cloudDir = normalize(d + s * t * _CloudShadowOffset);
                CloudParams c;
                c.coverage = _CG_Coverage; c.coverageScale = _CG_CoverageScale; c.coverageSoftness = _CG_CoverageSoftness;
                c.detailScale = _CG_DetailScale; c.warp = _CG_Warp; c.puffiness = _CG_Puffiness; c.puffScale = _CG_PuffScale;
                c.threshold = _CG_Threshold; c.edgeSoftness = _CG_EdgeSoftness; c.speed = _CG_Speed; c.evolve = _CG_Evolve;
                c.highScale = _CG_HighScale; c.highStretch = _CG_HighStretch; c.highThreshold = _CG_HighThreshold; c.highSpeed = _CG_HighSpeed;
                float footprint = length(fwidth(d)) + _CloudShadowSoftness * 0.004;   // softness = blur the shape
                float puff;
                float low = CloudLowDensity(cloudDir, _Time.y, 4, footprint, c, puff) * _CG_Opacity;
                // The high layer sits a little higher: its shadow lands a little further from it.
                float3 highDir = normalize(d + s * (t + _CG_HighParallax) * _CloudShadowOffset);
                float high = CloudHighDensity(highDir, _Time.y, footprint, c) * _CG_HighOpacity;
                return 1 - (1 - low) * (1 - high);
            }
            #endif

            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.tangentWS = float4(nrm.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half maskSmooth = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, i.uv).a;

                // Ocean roughness varies with how dark the water is in the satellite image
                // (deep water vs. shallow shelves and sediment). Brightness is judged on a perceptual
                // scale, from a slightly blurred lookup so the variation is smooth, not pixel noise.
                half ocean = smoothstep(0.3h, 0.6h, maskSmooth);                       // mask: ocean ~0.82, land ~0.12
                half3 blurred = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, i.uv, _OceanVariationBlur).rgb;
                half bright = pow(max(dot(blurred, half3(0.2126h, 0.7152h, 0.0722h)), 1e-5h), 1.0h / 2.2h);
                half t = saturate((bright - _OceanDarkLevel) / max(_OceanBrightLevel - _OceanDarkLevel, 1e-3h));
                t = pow(t, _OceanVariationContrast);
                half oceanSmooth = lerp(_OceanSmoothDark, _OceanSmoothBright, t);
                half smoothness = lerp(maskSmooth, oceanSmooth, ocean * _OceanVariation) * _Smoothness;

                Light sun = GetMainLight();
                float3 geoN = normalize(i.normalWS);                 // the smooth sphere
                float3 viewDir = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // How far inland this point is: the ocean mask, blurred over the coast width, gives the share
                // of land around it (about 0.5 on the shoreline, 1 deep inland).
                half nearMask = SAMPLE_TEXTURE2D_BIAS(_MetallicGlossMap, sampler_MetallicGlossMap, i.uv, _CoastWidth).a;
                half landAround = 1 - smoothstep(0.3h, 0.6h, nearMask);
                half inland = pow(saturate((landAround - 0.5h) * 2), _CoastFalloff);

                // Relief from the normal map (flat on the ocean already). Blurred by sampling a softer mip:
                // more at the coast, less inland, with strength fading toward the shore.
                half blur = lerp(_CoastBlur, _ReliefBlur, inland);
                half strength = _BumpScale * lerp(_CoastStrength, 1, inland) * (1 + blur * _BlurCompensation);
                strength *= 1 - ocean;                                                   // relief on land only, never on the sea

                // Work in slopes (not raw normal x/y) so strength scales evenly and the steepest texels
                // can't lie flat and turn black; the slope is capped by Max Slope.
                half3 raw = UnpackNormal(SAMPLE_TEXTURE2D_BIAS(_BumpMap, sampler_BumpMap, i.uv, blur));
                half2 slope = raw.xy / max(raw.z, 0.05h) * strength;
                half slopeLen = length(slope);
                slope *= min(1, _MaxSlope / max(slopeLen, 1e-4h));
                half3 nTS = normalize(half3(slope, 1));
                float3 bitangent = i.tangentWS.w * cross(i.normalWS, i.tangentWS.xyz);
                float3 bumpN = normalize(TransformTangentToWorld(nTS, half3x3(i.tangentWS.xyz, bitangent, i.normalWS)));

                // Fade the relief out as the Sun sinks toward this point's horizon: full relief in daylight,
                // smooth sphere at the day/night line and beyond, so the night side can't catch sunlight.
                half sunHeight = dot(geoN, sun.direction);
                half relief = saturate(sunHeight / _NormalFade);
                float3 n = normalize(lerp(geoN, bumpN, relief));

                // Same direct lighting as URP Lit (dielectric, smoothness from the mask).
                BRDFData brdf;
                half alpha = 1;
                InitializeBRDFData(albedo, 0, half3(0, 0, 0), smoothness, alpha, brdf);
                half3 col = LightingPhysicallyBased(brdf, sun, n, viewDir);
                #if defined(_CLOUD_SHADOWS)
                col *= 1 - CloudShadow(i.positionWS, sun.direction) * _CloudShadowStrength;
                #endif

                // Soft ambient so the night side isn't pure black (no sky reflections: it's space).
                col += SampleSH(geoN) * albedo * _AmbientStrength;
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half DepthFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings DNVert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 DNFrag(Varyings i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
}
