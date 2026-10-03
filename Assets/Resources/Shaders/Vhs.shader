Shader "RealityDirector/Vhs"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _VhsTime ("Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _VhsTime;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _VhsTime;
                float2 uv = i.uv;

                float row = floor(uv.y * 220.0);
                uv.x += (Hash(float2(row, floor(t * 11.0))) - 0.5) * 0.014;

                float sweep = frac(uv.y * 1.7 + t * 0.23);
                float tear = smoothstep(0.0, 0.02, sweep) * smoothstep(0.16, 0.03, sweep);
                uv.x += tear * 0.07 * sin(t * 6.5 + row);

                float2 red = uv + float2(0.0055, 0.0);
                float2 blue = uv - float2(0.0055, 0.001);
                fixed r = tex2D(_MainTex, red).r;
                fixed g = tex2D(_MainTex, uv).g;
                fixed b = tex2D(_MainTex, blue).b;
                fixed a = tex2D(_MainTex, uv).a;
                float3 col = float3(r, g, b);

                float luma = dot(col, float3(0.3, 0.59, 0.11));
                col = lerp(col, luma.xxx, 0.28);
                col = col * float3(1.05, 0.92, 1.08) + float3(0.03, 0.01, 0.04);

                float scan = 0.78 + 0.22 * step(0.5, frac(uv.y * 240.0));
                col *= scan;

                float grain = Hash(float2(uv.x * 520.0 + t * 19.0, uv.y * 520.0 - t * 13.0));
                col += (grain - 0.5) * 0.16;

                float roll = frac(uv.y - t * 0.18);
                float bar = smoothstep(0.0, 0.008, roll) * smoothstep(0.045, 0.012, roll);
                col = lerp(col, float3(0.92, 0.9, 0.96), bar * 0.7);
                col += tear * float3(0.15, 0.12, 0.18);

                float2 d = uv - 0.5;
                float vig = smoothstep(0.72, 0.22, dot(d, d));
                col *= lerp(0.45, 1.0, vig);

                return fixed4(saturate(col), a) * i.color;
            }
            ENDCG
        }
    }
}
