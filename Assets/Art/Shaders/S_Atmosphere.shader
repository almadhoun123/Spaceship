// Planet atmosphere as a radial volume: put it on a sphere slightly larger than the planet
// (same centre, uniform scale). For each pixel it marches the view ray through the shell between
// the outer sphere and the planet surface, with air thinning out with height, so the limb glows
// and the disk gets a light haze.
//
// It is not self-lit: every sample takes its brightness from the main light (the Sun), so the
// night side stays dark and the atmosphere fades out there. No clouds.
//
// The camera is expected to be outside the shell (back faces are culled).
Shader "Spaceships/Atmosphere"
{
    Properties
    {
        _DayColor ("Air Color (lit)", Color) = (0.35, 0.62, 1.0, 1)
        _Intensity ("Brightness", Range(0, 10)) = 1.0
        _Density ("Thickness (opacity)", Range(0, 10)) = 0.6
        _Falloff ("Height Falloff (higher = thinner at the top)", Range(0.5, 12)) = 4
        _PlanetRadius ("Planet Radius (fraction of this sphere)", Range(0.5, 1)) = 0.962
        _NightOpacity ("Opacity on the Night Side", Range(0, 1)) = 0.1
        _SunsetWidth ("Day/Night Softness (how gradually the air goes dark)", Range(0.01, 1)) = 0.25

        [Header(Day and night line tint)]
        _TerminatorColor ("Color (day side of the band)", Color) = (1.0, 0.55, 0.3, 1)
        _TerminatorColor2 ("Color (night side of the band)", Color) = (1.0, 0.55, 0.3, 1)
        _TerminatorTint ("Amount", Range(0, 1)) = 0.2
        _TerminatorOffset ("Position (- toward night, + toward day)", Range(-0.5, 0.5)) = 0.05
        _TerminatorWidth ("Width", Range(0.01, 1)) = 0.125
        _TerminatorSharpness ("Sharpness (higher = thinner core)", Range(0.25, 8)) = 2
        _TerminatorBrightness ("Glow (1 = no extra light)", Range(0, 6)) = 1
        _TerminatorLimbOnly ("Only at the Horizon Rim", Range(0, 1)) = 0
        _TerminatorNightReach ("Reach Into the Night Side", Range(0, 1)) = 0
        _ForwardGlow ("Glow Toward the Sun", Range(0, 4)) = 1.2
        [IntRange] _Steps ("Ray Steps", Range(4, 32)) = 16
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha      // premultiplied: adds lit air, slightly veils what's behind
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DayColor;
                half4 _TerminatorColor;
                half4 _TerminatorColor2;
                half _TerminatorTint;
                half _TerminatorOffset;
                half _TerminatorWidth;
                half _TerminatorSharpness;
                half _TerminatorBrightness;
                half _TerminatorLimbOnly;
                half _TerminatorNightReach;
                half _Intensity;
                half _Density;
                half _Falloff;
                half _PlanetRadius;
                half _NightOpacity;
                half _SunsetWidth;
                half _ForwardGlow;
                float _Steps;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            // Ray / sphere: returns (near, far) distances, or far < near when missed.
            float2 RaySphere(float3 ro, float3 rd, float3 c, float r)
            {
                float3 oc = ro - c;
                float b = dot(oc, rd);
                float h = b * b - (dot(oc, oc) - r * r);
                if (h < 0) return float2(1, -1);
                h = sqrt(h);
                return float2(-b - h, -b + h);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 centre = TransformObjectToWorld(float3(0, 0, 0));
                float outerR = length(TransformObjectToWorld(float3(0.5, 0, 0)) - centre);   // mesh radius is 0.5
                float innerR = outerR * _PlanetRadius;

                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.positionWS - ro);

                // Segment of the view ray inside the air: from entering the shell to leaving it,
                // or to hitting the planet, whichever comes first.
                float2 outer = RaySphere(ro, rd, centre, outerR);
                float2 inner = RaySphere(ro, rd, centre, innerR);
                float tStart = max(outer.x, 0);
                float tEnd = outer.y;
                if (inner.y >= inner.x && inner.x > 0) tEnd = min(tEnd, inner.x);
                if (tEnd <= tStart) return 0;

                Light sun = GetMainLight();
                float3 L = sun.direction;
                int steps = (int)_Steps;
                float ds = (tEnd - tStart) / steps;
                float thickness = outerR - innerR;

                float depth = 0;        // optical depth along the ray
                float3 light = 0;       // sunlight scattered toward the camera
                float sunlit = 0;       // how much of the air on this ray is in daylight
                [loop]
                for (int s = 0; s < steps; s++)
                {
                    float3 p = ro + rd * (tStart + ds * (s + 0.5));
                    float3 up = (p - centre);
                    float r = length(up);
                    up /= r;
                    float h = saturate((r - innerR) / thickness);
                    float dens = exp(-h * _Falloff) * (1 - h) * (ds / thickness);

                    // Daylight at this point of the air (soft day/night line).
                    float sunHeight = dot(up, L);
                    float day = smoothstep(-_SunsetWidth * 0.5, _SunsetWidth, sunHeight);

                    // Tint band along the day/night line: -1 at its night edge, +1 at its day edge.
                    float bandX = (sunHeight - _TerminatorOffset) / _TerminatorWidth;
                    float band = pow(saturate(1 - abs(bandX)), _TerminatorSharpness);
                    float3 bandColor = lerp(_TerminatorColor2.rgb, _TerminatorColor.rgb, saturate(bandX * 0.5 + 0.5));
                    float graze = 1 - abs(dot(rd, up));                         // 1 when looking along the horizon
                    band *= lerp(1, graze * graze, _TerminatorLimbOnly);
                    float bandAmount = band * _TerminatorTint;
                    float3 tint = lerp(_DayColor.rgb, bandColor, bandAmount);
                    float glow = 1 + bandAmount * (_TerminatorBrightness - 1);
                    float lit = max(day, band * _TerminatorNightReach);          // band light can spill past the shadow

                    // Light that already passed through the air in front of this sample is dimmed.
                    float transmit = exp(-depth * _Density);
                    light += tint * lit * glow * dens * transmit;
                    sunlit += lit * dens;
                    depth += dens;
                }

                // Brighter when looking roughly toward the Sun through the air.
                float phase = 1 + _ForwardGlow * pow(saturate(dot(rd, L)), 6);
                float3 col = light * phase * _Intensity * sun.color;

                // Opacity: the air veils the planet only where it's lit; on the night side it nearly vanishes.
                float dayShare = sunlit / max(depth, 1e-5);
                float alpha = (1 - exp(-depth * _Density)) * lerp(_NightOpacity, 1, dayShare);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
