// PS1 görünümlü URP malzeme shader'ı.
// - Köşe titremesi: köşeler ekranda sanal bir piksel ızgarasına yuvarlanır.
// - Eğri doku (affine): doku koordinatları perspektif düzeltmesi olmadan, isteğe bağlı oranda.
// - Köşe bazlı ışık: ana ışık, ek ışıklar ve ortam ışığı köşelerde hesaplanıp yüzeye yayılır.
// - Sis: Unity sis ayarları. Gerçek zamanlı gölge yok (PS1'de de yoktu).
Shader "Retro/Lit"
{
    Properties
    {
        _BaseMap ("Doku", 2D) = "white" {}
        _BaseColor ("Renk", Color) = (1, 1, 1, 1)
        [Header(PS1)]
        _SnapResolution ("Köşe ızgarası (sanal ekran pikseli)", Vector) = (480, 270, 0, 0)
        _AffineAmount ("Eğri doku miktarı", Range(0, 1)) = 0.6
        _Emission ("Kendi ışığı (lamba gibi yüzeyler)", Color) = (0, 0, 0, 0)
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
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _SnapResolution;
                float _AffineAmount;
                half4 _Emission;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;          // Perspektif düzeltmeli doku koordinatı
                float3 affineUV : TEXCOORD1;    // xy = uv * w, z = w (düzeltmesiz koordinat için)
                half3 lighting : TEXCOORD2;     // Köşede hesaplanan ışık
                half fogFactor : TEXCOORD3;
            };

            // Half-Lambert: dot'u -1..1 yerine 0..1'e taşıyıp kare alır (Valve, Half-Life).
            half WrapDiffuse(float3 normal, float3 lightDir)
            {
                half wrap = dot(normal, lightDir) * 0.5 + 0.5;
                return wrap * wrap;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                // Köşe titremesi: ekran uzayında ızgaraya yuvarla.
                float2 grid = max(_SnapResolution.xy * 0.5, 1.0);
                positionCS.xy = round(positionCS.xy / positionCS.w * grid) / grid * positionCS.w;
                output.positionCS = positionCS;

                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uv = uv;
                output.affineUV = float3(uv * positionCS.w, positionCS.w);

                // Köşe bazlı ışık. Yumuşak (half-Lambert): ışık arka yüzlere de "sarılır";
                // ışığa bakmayan yüzler simsiyah olmak yerine kademeli koyulaşır.
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                Light mainLight = GetMainLight();
                half3 light = mainLight.color * WrapDiffuse(normalWS, mainLight.direction);

                uint additionalCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < additionalCount; ++i)
                {
                    Light extra = GetAdditionalLight(i, positionWS);
                    light += extra.color * extra.distanceAttenuation * WrapDiffuse(normalWS, extra.direction);
                }

                light += SampleSH(normalWS); // Ortam ışığı
                output.lighting = light;

                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 affine = input.affineUV.xy / input.affineUV.z;
                float2 uv = lerp(input.uv, affine, _AffineAmount);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half3 color = albedo.rgb * input.lighting + _Emission.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Derinlik geçişi (URP bazı efektler için ister). Köşe titremesi burada da uygulanır ki derinlik uyuşsun.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _SnapResolution;
                float _AffineAmount;
                half4 _Emission;
            CBUFFER_END

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                float4 positionCS = TransformObjectToHClip(positionOS.xyz);
                float2 grid = max(_SnapResolution.xy * 0.5, 1.0);
                positionCS.xy = round(positionCS.xy / positionCS.w * grid) / grid * positionCS.w;
                return positionCS;
            }

            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
