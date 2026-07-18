// Subtle silhouette outline (not full cel-shading): detects depth and normal
// discontinuities between neighboring pixels and darkens edges toward
// _OutlineColor. Designed to sit after Ps2Pixelate in the Renderer Features
// list so the outline reads against the already-pixelated image. Assign as
// the Material on a second URP "Full Screen Pass Renderer Feature" with
// Requirements set to Depth + Normal.
Shader "Custom/Ps2Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.03, 0.02, 0.02, 1)
        _OutlineThickness ("Outline Thickness (px)", Range(0.5, 4)) = 1.5
        _DepthThreshold ("Depth Threshold", Range(0.0001, 0.1)) = 0.01
        _NormalThreshold ("Normal Threshold", Range(0.01, 1)) = 0.4
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "Ps2Outline"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            half4 _OutlineColor;
            float _OutlineThickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _OutlineStrength;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 baseColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);

                float2 texel = _BlitTexture_TexelSize.xy * _OutlineThickness;

                float depthC = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float depthL = LinearEyeDepth(SampleSceneDepth(uv - float2(texel.x, 0)), _ZBufferParams);
                float depthR = LinearEyeDepth(SampleSceneDepth(uv + float2(texel.x, 0)), _ZBufferParams);
                float depthU = LinearEyeDepth(SampleSceneDepth(uv + float2(0, texel.y)), _ZBufferParams);
                float depthD = LinearEyeDepth(SampleSceneDepth(uv - float2(0, texel.y)), _ZBufferParams);

                float depthEdge = abs(depthL - depthC) + abs(depthR - depthC) + abs(depthU - depthC) + abs(depthD - depthC);
                depthEdge = step(_DepthThreshold, depthEdge);

                float3 normalC = SampleSceneNormals(uv);
                float3 normalL = SampleSceneNormals(uv - float2(texel.x, 0));
                float3 normalR = SampleSceneNormals(uv + float2(texel.x, 0));
                float3 normalU = SampleSceneNormals(uv + float2(0, texel.y));
                float3 normalD = SampleSceneNormals(uv - float2(0, texel.y));

                float normalEdge = (1 - dot(normalC, normalL)) + (1 - dot(normalC, normalR))
                                  + (1 - dot(normalC, normalU)) + (1 - dot(normalC, normalD));
                normalEdge = step(_NormalThreshold, normalEdge);

                float edge = saturate(depthEdge + normalEdge);

                half4 result = lerp(baseColor, _OutlineColor, edge * _OutlineStrength);
                result.a = 1.0;
                return result;
            }
            ENDHLSL
        }
    }
}
