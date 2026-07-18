// PS2-style presentation pass (GDD §07 "Render"): quantizes screen UVs to a low
// internal resolution before sampling (blocky low-res look without a separate
// downscaled render target), then posterizes color depth with a light ordered
// dither to avoid harsh banding. Designed to be assigned as the Material on a
// URP "Full Screen Pass Renderer Feature" (Blit-based fullscreen pass).
Shader "Custom/Ps2Pixelate"
{
    Properties
    {
        _Resolution ("Internal Resolution (px)", Vector) = (640, 360, 0, 0)
        _PosterizeLevels ("Posterize Levels Per Channel", Range(4, 256)) = 48
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Ps2Pixelate"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float2 _Resolution;
            float _PosterizeLevels;
            float _DitherStrength;

            // 4x4 Bayer ordered-dither matrix, normalized to [0, 1).
            static const float Bayer4x4[16] =
            {
                 0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
                12.0 / 16.0,  4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
                 3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
                15.0 / 16.0,  7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
            };

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 res = max(_Resolution, float2(1, 1));
                float2 pixelUV = (floor(input.texcoord.xy * res) + 0.5) / res;

                half4 color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, pixelUV, _BlitMipLevel);

                uint2 ditherCoord = uint2(input.positionCS.xy) % 4;
                float dither = (Bayer4x4[ditherCoord.y * 4 + ditherCoord.x] - 0.5) * _DitherStrength;

                float levels = max(_PosterizeLevels, 2.0);
                color.rgb = saturate(color.rgb + dither / levels);
                color.rgb = floor(color.rgb * levels + 0.5) / levels;
                color.a = 1.0;

                return color;
            }
            ENDHLSL
        }
    }
}
