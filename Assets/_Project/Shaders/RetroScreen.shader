// Düşük çözünürlüklü oyun görüntüsünü ekrana basarken PS1 benzeri renk azaltma ve
// sıralı (Bayer 4x4) dithering uygular. RawImage üzerinde kullanılan bir UI shader'ıdır.
Shader "Retro/Screen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Görüntü", 2D) = "white" {}
        _ColorLevels ("Kanal başına renk seviyesi", Range(2, 256)) = 32
        _DitherStrength ("Dithering gücü", Range(0, 1)) = 0.25
        _Desaturate ("Renksizleştirme", Range(0, 1)) = 0
        _Tint ("Renksizken ton", Color) = (1, 0.35, 0.3, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "IgnoreProjector" = "True" "RenderType" = "Opaque" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x = 1/genişlik, y = 1/yükseklik, z = genişlik, w = yükseklik
            float _ColorLevels;
            float _DitherStrength;
            float _Desaturate;
            float4 _Tint;

            // 4x4 Bayer matrisi (0..15), 0..1 aralığına ölçeklenir.
            static const float bayer[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 color = tex2D(_MainTex, i.uv).rgb;

                // Ölüm efekti: renkler parlaklığa (gri) indirgenir ve tona boyanır.
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, luminance * _Tint.rgb, _Desaturate);

                // Desen düşük çözünürlüklü görüntünün pikseline göre: ekran ne kadar büyük olursa olsun aynı kalır.
                uint2 pixel = uint2(floor(i.uv * _MainTex_TexelSize.zw)) % 4u;
                float threshold = (bayer[pixel.y * 4 + pixel.x] + 0.5) / 16.0 - 0.5;

                float steps = _ColorLevels - 1.0;
                color = floor(color * steps + 0.5 + threshold * _DitherStrength) / steps;
                return fixed4(saturate(color), 1.0);
            }
            ENDCG
        }
    }
}
