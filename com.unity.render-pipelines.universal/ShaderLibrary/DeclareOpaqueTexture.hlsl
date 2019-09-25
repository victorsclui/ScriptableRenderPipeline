#ifndef UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED
#define UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
TEXTURE2D_ARRAY(_CameraOpaqueTexture);
#else
TEXTURE2D(_CameraOpaqueTexture);
#endif
SAMPLER(sampler_CameraOpaqueTexture);

float3 SampleSceneColor(float2 uv)
{
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return SAMPLE_TEXTURE2D_ARRAY(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv, unity_StereoEyeIndex);
#else
    return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv));
#endif
}

float3 LoadSceneColor(int2 uv)
{
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return LOAD_TEXTURE2D_ARRAY(_CameraOpaqueTexture, uv, unity_StereoEyeIndex);
#else
    return LOAD_TEXTURE2D(_CameraOpaqueTexture, uv);
#endif
}

#endif
