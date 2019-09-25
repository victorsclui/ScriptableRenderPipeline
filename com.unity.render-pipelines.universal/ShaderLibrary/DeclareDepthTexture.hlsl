#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
TEXTURE2D_ARRAY(_CameraDepthTexture);
#else
TEXTURE2D(_CameraDepthTexture);
#endif
SAMPLER(sampler_CameraDepthTexture);

float SampleSceneDepth(float2 uv)
{
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r;
#else
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
#endif
}

float LoadSceneDepth(int2 uv)
{
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return LOAD_TEXTURE2D_ARRAY(_CameraDepthTexture, uv, unity_StereoEyeIndex).r;
#else
    return LOAD_TEXTURE2D(_CameraDepthTexture, uv).r;
#endif
}

#endif
