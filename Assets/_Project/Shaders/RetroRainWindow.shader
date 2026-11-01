// Pencerenin dışı: gece manzarası resmi + camdaki yağmur.
// Manzara bakış açısına göre kayar (paralaks): dışarısı gerçekten uzaktaymış gibi görünür.
// Şimşek: _RainFlash genel değişkenini Lightning bileşeni yazar.
Shader "Retro/RainWindow"
{
    Properties
    {
        _NightMap ("Gece manzarası", 2D) = "black" {}
        _DropMap ("Damla dokusu (RG: kırılma, B: yağmur çizgisi)", 2D) = "gray" {}
        _Depth ("Manzara uzaklığı", Float) = 0.6
        _Refraction ("Damla kırılması", Float) = 0.08
        _RunSpeed ("Camdaki damlaların akışı", Float) = 0.12
        _RainSpeed ("Yağmur hızı", Float) = 1.7
        _RainAmount ("Yağmur çizgileri", Range(0, 1)) = 0.3
        _Tint ("Renk", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NightMap);
            SAMPLER(sampler_NightMap);
            TEXTURE2D(_DropMap);
            SAMPLER(sampler_DropMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NightMap_ST;
                float4 _DropMap_ST;
                float _Depth;
                float _Refraction;
                float _RunSpeed;
                float _RainSpeed;
                float _RainAmount;
                half4 _Tint;
            CBUFFER_END

            float _RainFlash;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewOS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.viewOS = TransformWorldToObject(GetCameraPositionWS()) - input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                float3 view = normalize(input.viewOS);
                float2 parallax = -view.xy / max(abs(view.z), 0.2) * _Depth * 0.15;

                float2 dropUV = uv * _DropMap_ST.xy;
                half2 slow = SAMPLE_TEXTURE2D(_DropMap, sampler_DropMap, dropUV + float2(0.0, time * _RunSpeed)).rg - 0.5;
                half2 fast = SAMPLE_TEXTURE2D(_DropMap, sampler_DropMap, dropUV * 0.6 + float2(0.31, time * _RunSpeed * 2.3)).rg - 0.5;
                float2 refract = (slow + fast) * _Refraction;

                half3 color = SAMPLE_TEXTURE2D(_NightMap, sampler_NightMap, uv * _NightMap_ST.xy + _NightMap_ST.zw + parallax + refract).rgb;

                float2 rainUV = (uv + parallax * 0.4) * float2(_DropMap_ST.x * 2.0, _DropMap_ST.y * 0.5);
                half rain = SAMPLE_TEXTURE2D(_DropMap, sampler_DropMap, rainUV + float2(0.0, time * _RainSpeed)).b;
                color += rain * _RainAmount * half3(0.55, 0.62, 0.75);
                color += (abs(slow.x) + abs(slow.y)) * half3(0.012, 0.014, 0.02);

                color += _RainFlash * half3(0.55, 0.6, 0.8) * (0.4 + color);
                return half4(color * _Tint.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
