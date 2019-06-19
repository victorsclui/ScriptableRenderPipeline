#ifndef LIGHTLOOP_VX_SHADOWS_HLSL
#define LIGHTLOOP_VX_SHADOWS_HLSL

#include "Packages/com.unity.voxelized-shadows/ShaderLibrary/Common.hlsl"

float GetDirectionalVxShadowAttenuation(float3 positionWS, DirectionalLightData light)
{
    bool vxShadowsEnabled = IsVxShadowsEnabled(light.vxShadowsBitset);

    if (vxShadowsEnabled)
    {
        uint begin = MaskBitsetVxShadowMapBegin(light.vxShadowsBitset);
        float attenuation = NearestSampleVxShadowing(begin, positionWS + _WorldSpaceCameraPos);

        return attenuation;
    }
    else
    {
        return 1.0;
    }
}

#endif // LIGHTLOOP_VX_SHADOWS_HLSL
