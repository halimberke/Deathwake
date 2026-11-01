// Saydam cam ve su. Köşe titremesi yok; sahnedeki küçük nesneler için.
Shader "Retro/Glass"
{
    Properties
    {
        _BaseColor ("Renk (alfa = saydamlık)", Color) = (0.7, 0.85, 0.95, 0.3)
        _RimPower ("Kenar parlaması", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _RimPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                half3 ambient : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);

                half3 light = SampleSH(output.normalWS);
                uint count = GetAdditionalLightsCount();
                for (uint i = 0u; i < count; ++i)
                {
                    Light extra = GetAdditionalLight(i, positionWS);
                    light += extra.color * extra.distanceAttenuation * 0.6;
                }

                output.ambient = light;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half facing = abs(dot(normalize(input.normalWS), normalize(input.viewWS)));
                half rim = pow(1.0 - facing, _RimPower);
                half alpha = saturate(_BaseColor.a + rim * 0.6);
                return half4(_BaseColor.rgb * input.ambient + rim * 0.15, alpha);
            }
            ENDHLSL
        }
    }
}
