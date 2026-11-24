// Bruma y rayos de luz: transparente, sin iluminacion, doble cara, se desvanece cerca de la camara (sin saltos).
// Mezcla configurable: alfa normal (bruma) o aditivo (rayos de sol).
Shader "HuesoOxido/AtmosphereSoft"
{
    Properties
    {
        _MainTex ("Textura suave (alfa)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0.1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
        _NearFade ("Desvanecer cerca (m)", Vector) = (2, 7, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Atmosphere"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _NearFade;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float dist : TEXCOORD1; };

            V vert(A v)
            {
                V o;
                float3 wp = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(wp);
                o.uv = v.uv;
                o.color = v.color;
                o.dist = distance(_WorldSpaceCameraPos, wp);
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half fade = saturate((i.dist - _NearFade.x) / max(0.01, _NearFade.y - _NearFade.x));
                half4 c = _Color * i.color;
                c.a *= t.a * fade;
                return c;
            }
            ENDHLSL
        }
    }
}
