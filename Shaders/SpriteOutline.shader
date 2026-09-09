// Procedural pixel-perfect sprite outline for URP 2D.
//
// Why: baked-in-art outlines lose pixels under Bilinear/Mip filtering or when a sprite's on-screen
// size doesn't land on an exact integer multiple of the source texture, so they vanish or gap out at
// certain scales. This shader instead measures, per fragment, the distance in *source texture texels*
// to the nearest opaque texel, and recomputes that every draw - so the outline is always exactly N
// whole texels wide regardless of GameObject scale, camera zoom, or Pixel Perfect Camera's
// upscale factor. Requires the sprite's Texture Import Settings to use Filter Mode: Point (no clamp,
// no bilinear/mip blur) - otherwise the source alpha itself is already blurred before this shader
// ever sees it. Also requires enough padding around the sprite in its atlas/sheet (at least the total
// stacked outline width, in texels) so neighbor sampling doesn't bleed into an adjacent packed sprite.
//
// Supports up to 3 stacked outline rings (innermost -> outermost). Leave a layer's Width at 0 to
// disable it - e.g. set only Outline 1 for a single outline, or all three for a tri-color ring.
Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.1

        [Header(Outline 1 - innermost, against the sprite edge)]
        _OutlineColor1 ("Color", Color) = (0, 0, 0, 1)
        _OutlineWidth1 ("Width (texels)", Range(0, 8)) = 1

        [Header(Outline 2)]
        _OutlineColor2 ("Color", Color) = (1, 1, 1, 1)
        _OutlineWidth2 ("Width (texels)", Range(0, 8)) = 0

        [Header(Outline 3 - outermost)]
        _OutlineColor3 ("Color", Color) = (1, 1, 1, 1)
        _OutlineWidth3 ("Width (texels)", Range(0, 8)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _AlphaCutoff;
            float4 _OutlineColor1, _OutlineColor2, _OutlineColor3;
            float _OutlineWidth1, _OutlineWidth2, _OutlineWidth3;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            // Distance, in whole source-texture texels, from uv to the nearest texel whose alpha
            // clears _AlphaCutoff - capped at maxRadius (texels beyond it aren't scanned, since no
            // outline layer needs to see further than its own cumulative width).
            float NearestOpaqueTexelDistance(float2 uv, float maxRadius)
            {
                float2 texel = _MainTex_TexelSize.xy;
                float maxRadiusSqr = maxRadius * maxRadius;
                float bestDistSqr = maxRadiusSqr + 1;

                int r = (int)ceil(maxRadius);
                for (int y = -r; y <= r; y++)
                {
                    for (int x = -r; x <= r; x++)
                    {
                        float distSqr = (float)(x * x + y * y);
                        if (distSqr > maxRadiusSqr || distSqr >= bestDistSqr)
                            continue;

                        if (SampleAlpha(uv + float2(x, y) * texel) > _AlphaCutoff)
                            bestDistSqr = distSqr;
                    }
                }

                return sqrt(bestDistSqr);
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                // Inside the sprite's own silhouette - draw it as-is, on top of every outline ring.
                if (texColor.a > _AlphaCutoff)
                    return texColor;

                float cumulative1 = _OutlineWidth1;
                float cumulative2 = cumulative1 + _OutlineWidth2;
                float cumulative3 = cumulative2 + _OutlineWidth3;
                float maxRadius = max(cumulative3, 0.0001);

                float dist = NearestOpaqueTexelDistance(i.uv, maxRadius);

                if (dist <= cumulative1)
                    return _OutlineColor1;
                if (dist <= cumulative2)
                    return _OutlineColor2;
                if (dist <= cumulative3)
                    return _OutlineColor3;

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
