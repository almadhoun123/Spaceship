// Simple, readable shading for blockout hazards (debris, enemy cones) floating in a black tunnel.
//  - Key light that follows the camera, so the side facing the players is always lit
//    and the form reads the same no matter how the ship turns.
//  - Rim light, so the silhouette stands out against the black background.
//  - Tip tint along the object's local +Z, so a cone shows which way it points.
//  - Distance fade, so near objects are bright and far ones dim (shows where they are).
Shader "Spaceships/ShadedHazard"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.45, 0.28, 0.13, 1)
        _Ambient ("Shadow Brightness", Range(0, 1)) = 0.18

        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 0.85, 0.6, 1)
        _RimPower ("Rim Width (higher is thinner)", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.8

        [Header(Pointing tip along local Z)]
        _TipColor ("Tip Color", Color) = (1, 0.9, 0.3, 1)
        _TipStrength ("Tip Strength (0 = off)", Range(0, 1)) = 0
        _TipStart ("Tip Start (local Z)", Float) = 0.3
        _TipEnd ("Tip End (local Z)", Float) = 1.75

        [Header(Distance fade)]
        _FadeStart ("Fade Start (m)", Float) = 40
        _FadeEnd ("Fade End (m)", Float) = 220
        _FarBrightness ("Far Brightness", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Ambient;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half4 _TipColor;
            half _TipStrength;
            float _TipStart;
            float _TipEnd;
            float _FadeStart;
            float _FadeEnd;
            half _FarBrightness;
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float tipZ : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tipZ = v.positionOS.z;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 toCam = _WorldSpaceCameraPos - i.positionWS;
                float dist = length(toCam);
                float3 v = toCam / max(dist, 1e-4);

                // Key light from just above and to the left of the camera.
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 l = normalize(v * 0.8 + camUp * 0.6 - camRight * 0.35);
                half diff = saturate(dot(n, l));
                diff = diff * diff * (3 - 2 * diff); // soft but clear light-to-shadow edge

                half3 col = _BaseColor.rgb * lerp(_Ambient, 1.0h, diff);

                // Tip tint: brighter toward the end the object points to.
                half tip = saturate((i.tipZ - _TipStart) / max(_TipEnd - _TipStart, 1e-4)) * _TipStrength;
                col = lerp(col, _TipColor.rgb * lerp(0.55h, 1.0h, diff), tip);

                // Rim light around the silhouette.
                half rim = pow(1 - saturate(dot(n, v)), _RimPower) * _RimStrength;
                col += _RimColor.rgb * rim;

                // Dim with distance so near and far read differently.
                half fade = 1 - saturate((dist - _FadeStart) / max(_FadeEnd - _FadeStart, 1e-4));
                col *= lerp(_FarBrightness, 1.0h, fade);

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
    }
}
