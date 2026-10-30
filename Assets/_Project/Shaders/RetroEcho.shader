// Yankının görünümü: eski bir kaydın oynatılması gibi.
// - Toplamalı (additive) saydam: karanlıkta parlar, arkası görünür.
// - Kenar parlaması (rim), yatay tarama çizgileri, titreme, ara sıra yatay kayma (glitch).
// - Kaba köşe titremesi: dünyadan daha "bozuk" durur.
// - _Materialize: 0 = yok, 1 = tam. Arada bloklu, aşağıdan yukarı belirme/dağılma.
// - _Glow: eylem uyarısı. 0 normal, 1+ parlak.
Shader "Retro/Echo"
{
    Properties
    {
        _Color ("Renk", Color) = (0.45, 0.85, 1, 0.35)
        _RimPower ("Kenar keskinliği", Range(0.5, 6)) = 2
        _SnapResolution ("Köşe ızgarası", Vector) = (240, 135, 0, 0)
        _GlitchStrength ("Kayma miktarı (m)", Range(0, 0.2)) = 0.06
        _Materialize ("Belirme", Range(0, 1)) = 1
        _Glow ("Eylem parlaması", Range(0, 2)) = 0
        _BaseHeight ("Ayak hizası (dünya y)", Float) = 0
        _Height ("Boy (m)", Float) = 1.9
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _RimPower;
                float4 _SnapResolution;
                float _GlitchStrength;
                float _Materialize;
                float _Glow;
                float _BaseHeight;
                float _Height;
            CBUFFER_END

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
                half fogFactor : TEXCOORD2;
            };

            float Hash(float3 p)
            {
                return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Yatay kayma: 10 cm'lik bantlar, saniyede 12 kez yeni rastgele değer; nadiren tetiklenir.
                float band = floor(positionWS.y * 10.0);
                float tick = floor(_Time.y * 12.0);
                float roll = Hash(float3(band, tick, 1.0));
                float shift = roll > 0.93 ? (Hash(float3(band, tick, 2.0)) - 0.5) * 2.0 * _GlitchStrength : 0.0;
                positionWS += TransformObjectToWorldDir(float3(1, 0, 0)) * shift;

                float4 positionCS = TransformWorldToHClip(positionWS);
                float2 grid = max(_SnapResolution.xy * 0.5, 1.0);
                positionCS.xy = round(positionCS.xy / positionCS.w * grid) / grid * positionCS.w;

                output.positionCS = positionCS;
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Belirme: 6 cm'lik bloklar, aşağıdan yukarı. Gürültü + yükseklik eşiği aşarsa piksel atılır.
                float3 block = floor(input.positionWS * 16.0);
                float height = saturate((input.positionWS.y - _BaseHeight) / _Height);
                float reveal = Hash(block) * 0.35 + height * 0.65;
                clip(_Materialize * 1.1 - reveal);
                float edge = saturate(1.0 - (_Materialize * 1.1 - reveal) * 12.0); // yeni beliren kenar parlar

                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDir)), _RimPower);

                // Tarama çizgisi: düşük çözünürlüklü görüntüde her iki satırdan biri sönük.
                float scan = fmod(floor(input.positionCS.y), 2.0) < 1.0 ? 1.0 : 0.6;
                float flicker = 0.85 + 0.15 * Hash(float3(floor(_Time.y * 20.0), 0, 0));

                float glow = _Glow + edge * 1.5;
                half3 color = _Color.rgb * (0.6 + rim * 1.4 + glow);
                half alpha = saturate(_Color.a * (0.5 + rim) * scan * flicker + glow * 0.35);

                // Toplamalı karışımda sis, rengi siyaha (hiçliğe) çeker.
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
