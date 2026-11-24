// Suelo del bosque: mezcla 5 capas PBR (bosque, pasto, camino, tierra roja y roca por pendiente) segun un mapa de control.
// Iluminacion URP completa (sombras, SSAO, luces adicionales, niebla). Compatible con SRP Batcher.
Shader "HuesoOxido/GroundSplat"
{
    Properties
    {
        _Control ("Control: R pasto, G camino, B tierra roja", 2D) = "black" {}
        _Macro ("Macro (variacion de color, 0.5 = neutro)", 2D) = "gray" {}
        _Diff0 ("Bosque", 2D) = "gray" {}  _Nor0 ("Bosque normal", 2D) = "bump" {}
        _Diff1 ("Pasto", 2D) = "gray" {}   _Nor1 ("Pasto normal", 2D) = "bump" {}
        _Diff2 ("Camino", 2D) = "gray" {}  _Nor2 ("Camino normal", 2D) = "bump" {}
        _Diff3 ("Tierra roja", 2D) = "gray" {} _Nor3 ("Tierra roja normal", 2D) = "bump" {}
        _DiffRock ("Roca", 2D) = "gray" {}
        _Tiling ("Metros por tile: bosque, pasto, camino, tierra", Vector) = (5, 4, 3, 5)
        _RockTiling ("Metros por tile roca", Float) = 6
        _WorldSize ("Tamano del mundo (m)", Float) = 320
        _NormalScale ("Fuerza de normales", Range(0, 2)) = 1
        _Smooth ("Suavidad: bosque, pasto, camino, tierra", Vector) = (0.10, 0.16, 0.08, 0.12)
        _RockSmooth ("Suavidad roca", Float) = 0.22
        _MacroStrength ("Fuerza del macro", Range(0, 1)) = 0.75
        _GrassTint ("Tinte del pasto", Color) = (0.78, 1.0, 0.55, 1)
        _ForestTint ("Tinte del bosque", Color) = (0.92, 1.0, 0.82, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Macro);   SAMPLER(sampler_Macro);
            TEXTURE2D(_Diff0); SAMPLER(sampler_Diff0);
            TEXTURE2D(_Diff1); TEXTURE2D(_Diff2); TEXTURE2D(_Diff3); TEXTURE2D(_DiffRock);
            TEXTURE2D(_Nor0); TEXTURE2D(_Nor1); TEXTURE2D(_Nor2); TEXTURE2D(_Nor3);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tiling;
                float4 _Smooth;
                float _RockTiling;
                float _WorldSize;
                float _NormalScale;
                float _RockSmooth;
                float _MacroStrength;
                float4 _GrassTint;
                float4 _ForestTint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // Dos escalas mezcladas rompen la repeticion visible del tile.
            half3 SampleDiff(TEXTURE2D_PARAM(tex, s), float2 wuv, float meters)
            {
                float2 uv = wuv / meters;
                half3 a = SAMPLE_TEXTURE2D(tex, s, uv).rgb;
                half3 b = SAMPLE_TEXTURE2D(tex, s, uv * 0.173 + float2(0.37, 0.71)).rgb;
                return lerp(a, b, 0.35);
            }

            half3 SampleNor(TEXTURE2D_PARAM(tex, s), float2 wuv, float meters)
            {
                return UnpackNormalScale(SAMPLE_TEXTURE2D(tex, s, wuv / meters), _NormalScale);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float2 wuv = i.positionWS.xz;
                float2 cuv = wuv / _WorldSize + 0.5;
                half4 ctrl = SAMPLE_TEXTURE2D(_Control, sampler_Control, cuv);
                half wGrass = ctrl.r, wRoad = ctrl.g, wRed = ctrl.b;
                half wForest = saturate(1.0 - wGrass - wRoad - wRed);
                half rock = saturate((0.80 - N.y) * 6.0);
                half k = 1.0 - rock;
                wForest *= k; wGrass *= k; wRoad *= k; wRed *= k;

                half3 d0 = SampleDiff(TEXTURE2D_ARGS(_Diff0, sampler_Diff0), wuv, _Tiling.x);
                half3 d1 = SampleDiff(TEXTURE2D_ARGS(_Diff1, sampler_Diff0), wuv, _Tiling.y);
                half3 d2 = SampleDiff(TEXTURE2D_ARGS(_Diff2, sampler_Diff0), wuv, _Tiling.z);
                half3 d3 = SampleDiff(TEXTURE2D_ARGS(_Diff3, sampler_Diff0), wuv, _Tiling.w);

                // Roca en pendiente: triplanar (el mapeo plano se estira en laderas).
                float3 bw = pow(abs(N), 4.0); bw /= (bw.x + bw.y + bw.z + 1e-5);
                float3 p = i.positionWS / _RockTiling;
                half3 dr = SAMPLE_TEXTURE2D(_DiffRock, sampler_Diff0, p.zy).rgb * bw.x
                         + SAMPLE_TEXTURE2D(_DiffRock, sampler_Diff0, p.xz).rgb * bw.y
                         + SAMPLE_TEXTURE2D(_DiffRock, sampler_Diff0, p.xy).rgb * bw.z;

                half3 albedo = d0 * _ForestTint.rgb * wForest + d1 * _GrassTint.rgb * wGrass + d2 * wRoad + d3 * wRed + dr * rock;
                half3 macro = SAMPLE_TEXTURE2D(_Macro, sampler_Macro, cuv).rgb * 2.0;
                albedo *= lerp(half3(1, 1, 1), macro, _MacroStrength);

                half3 n0 = SampleNor(TEXTURE2D_ARGS(_Nor0, sampler_Diff0), wuv, _Tiling.x);
                half3 n1 = SampleNor(TEXTURE2D_ARGS(_Nor1, sampler_Diff0), wuv, _Tiling.y);
                half3 n2 = SampleNor(TEXTURE2D_ARGS(_Nor2, sampler_Diff0), wuv, _Tiling.z);
                half3 n3 = SampleNor(TEXTURE2D_ARGS(_Nor3, sampler_Diff0), wuv, _Tiling.w);
                half3 nTS = normalize(n0 * wForest + n1 * wGrass + n2 * wRoad + n3 * wRed + half3(0, 0, 1) * rock);
                float3 T = normalize(float3(1, 0, 0) - N * N.x);
                float3 B = cross(T, N);
                float3 normalWS = normalize(T * nTS.x + B * nTS.y + N * nTS.z);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = 0;
                s.smoothness = _Smooth.x * wForest + _Smooth.y * wGrass + _Smooth.z * wRoad + _Smooth.w * wRed + _RockSmooth * rock;
                s.occlusion = 1;
                s.alpha = 1;
                s.normalTS = half3(0, 0, 1);

                InputData id = (InputData)0;
                id.positionWS = i.positionWS;
                id.normalWS = normalWS;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                id.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                id.fogCoord = i.fogFactor;
                id.vertexLighting = half3(0, 0, 0);
                id.bakedGI = SampleSH(normalWS);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                id.shadowMask = half4(1, 1, 1, 1);

                half4 col = UniversalFragmentPBR(id, s);
                col.rgb = MixFog(col.rgb, id.fogCoord);
                col.a = 1;
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half frag(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(V i) : SV_Target
            {
                float3 n = NormalizeNormalPerPixel(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    float2 remapped = saturate(oct * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
