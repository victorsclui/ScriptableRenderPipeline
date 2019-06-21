#ifndef UNITY_VX_SHADOWMAPS_COMMON_INCLUDED
#define UNITY_VX_SHADOWMAPS_COMMON_INCLUDED

#if defined(SHADER_API_METAL)
#define USE_EMULATE_COUNTBITS
#endif

#define OFFSET_DIR 15
#define OFFSET_POINT 15 // TODO : define it for point light
#define OFFSET_SPOT 15 // TODO : define it for spot light

#define VX_SHADOWS_ONLY  0x80000000
#define VX_SHADOWS_BLEND 0x40000000

#define VX_SHADOWS_LIT         0x00000001
#define VX_SHADOWS_SHADOWED    0x00000002
#define VX_SHADOWS_INTERSECTED 0x00000003

StructuredBuffer<uint> _VxShadowMapsBuffer;


uint emulateCLZ(uint x)
{
    // emulate it similar to count leading zero.
    // count leading 1bit.

    uint n = 32;
    uint y;

    y = x >> 16; if (y != 0) { n = n - 16; x = y; }
    y = x >>  8; if (y != 0) { n = n -  8; x = y; }
    y = x >>  4; if (y != 0) { n = n -  4; x = y; }
    y = x >>  2; if (y != 0) { n = n -  2; x = y; }
    y = x >>  1; if (y != 0) return n - 2;

    return n - x;
}

uint countBits(uint i)
{
#ifdef USE_EMULATE_COUNTBITS
    i = i - ((i >> 1) & 0x55555555);
    i = (i & 0x33333333) + ((i >> 2) & 0x33333333);

    return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
#else
    return countbits(i);
#endif
}

uint4 countBits(uint4 i)
{
#ifdef USE_EMULATE_COUNTBITS
    i = i - ((i >> 1) & 0x55555555);
    i = (i & 0x33333333) + ((i >> 2) & 0x33333333);

    return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
#else
    return countbits(i);
#endif
}

// todo : calculate uint2 and more?
uint CalculateRescale(uint srcPosbit, uint dstPosbit)
{
    return 32 - emulateCLZ(srcPosbit ^ dstPosbit);
}

uint4 AccessVxShadowMaps(uint4 vxsmAccess4)
{
    return uint4(
        _VxShadowMapsBuffer[vxsmAccess4.x],
        _VxShadowMapsBuffer[vxsmAccess4.y],
        _VxShadowMapsBuffer[vxsmAccess4.z],
        _VxShadowMapsBuffer[vxsmAccess4.w]);
}

uint4 SumDet(uint4 det01, uint4 det23)
{
    return uint4(
        det01.x + det01.y,
        det01.z + det01.w,
        det23.x + det23.y,
        det23.z + det23.w);
}

void TraverseVxShadowMapPosQ(uint begin, uint typeOffset, uint3 posQ, out uint2 result)
{
    uint vxsmOffset = begin + typeOffset;
    uint dagScale = _VxShadowMapsBuffer[begin + 2];

    uint scaleShift = dagScale - 2;
    uint nodeIndex = 0;
    uint shadowbit = 0;

    bool intersected = true;

    for (; scaleShift > 1 && intersected; --scaleShift)
    {
        // get childmask
        uint vxsmAccess = vxsmOffset + nodeIndex;
        uint childmask = _VxShadowMapsBuffer[vxsmAccess] >> 16;

        // calculate where to go to child
        uint3 childDet = ((posQ >> scaleShift) & 0x00000002) << uint3(0, 1, 2);
        uint cellShift = childDet.x + childDet.y + childDet.z;
        uint cellbit   = 0x00000003 << cellShift;

        // if it has lit and shadowed, it is not decided yet(need to traverse more)
        shadowbit = (childmask & cellbit) >> cellShift;
        intersected = shadowbit == 0x00000003;

        // find next child node
        uint mask = ~(0xFFFFFFFF << cellShift);
        uint childrenbit = childmask & ((childmask & 0x0000AAAA) >> 1);
        uint childIndex = countBits(childrenbit & mask) + 1;

        // go down to the next node
        vxsmAccess += childIndex;
        nodeIndex = _VxShadowMapsBuffer[vxsmAccess];
    }

#ifdef VX_SHADOWS_DEBUG
    result = uint2(nodeIndex, shadowbit);
#else
    uint scale = scaleShift + 2;
    result = uint2(nodeIndex, shadowbit);
#endif
}

void TraverseVxShadowMapPosQ2x2(uint begin, uint typeOffset, uint2 adjOffset, uint3 posQ, out uint4 results[2])
{
    uint vxsmOffset = begin + typeOffset;
    uint dagScale = _VxShadowMapsBuffer[begin + 2];

    uint4 posQ_01 = posQ.xyxy + adjOffset.xxyx; //uint4(0, 0, 1, 0);
    uint4 posQ_23 = posQ.xyxy + adjOffset.xyyy; //uint4(0, 1, 1, 1);

    uint scaleShift = dagScale - 2;
    uint4 nodeIndex4 = 0;
    uint4 shadowbit4 = 0;

    bool4 intersected4 = true;

    for (; scaleShift > 1 && any(intersected4); --scaleShift)
    {
        // calculate where to go to child
        uint4 childDet_01 = ((posQ_01 >> scaleShift) & 0x00000002) << uint4(0, 1, 0, 1);
        uint4 childDet_23 = ((posQ_23 >> scaleShift) & 0x00000002) << uint4(0, 1, 0, 1);
        uint  childDet_z  = ((posQ.z  >> scaleShift) & 0x00000002) << 2;
        uint4 cellShift4 = SumDet(childDet_01, childDet_23) + childDet_z;
        uint4 cellbit4 = 0x00000003 << cellShift4;
        uint4 mask4 = ~(0xFFFFFFFF << cellShift4);

        // get childmask
        uint4 vxsmAccess4 = vxsmOffset + nodeIndex4;
        uint4 childmask4 = AccessVxShadowMaps(vxsmAccess4) >> 16;

        // find next child node
        uint4 childrenbit4 = childmask4 & ((childmask4 & 0x0000AAAA) >> 1);
        uint4 childIndex4 = countBits(childrenbit4 & mask4) + 1;

        // if it has lit and shadowed, it is not decided yet(need to traverse more)
        shadowbit4 = intersected4 ? (childmask4 & cellbit4) >> cellShift4 : shadowbit4;
        intersected4 = shadowbit4 == 0x00000003;

        // go down to the next node
        vxsmAccess4 += childIndex4;
        nodeIndex4 = intersected4 ? AccessVxShadowMaps(vxsmAccess4) : nodeIndex4;
    }

#ifdef VX_SHADOWS_DEBUG
    uint scale = scaleShift + 2;
    results[0] = nodeIndex4;
    results[1] = shadowbit4;
#else
    results[0] = nodeIndex4;
    results[1] = shadowbit4;
#endif
}

void TraverseVxShadowMapPosQ2x2x2(uint begin, uint typeOffset, uint2 adjOffset, uint3 posQ, out uint4 results[4])
{
    uint vxsmOffset = begin + typeOffset;
    uint dagScale = _VxShadowMapsBuffer[begin + 2];

    uint4 posQ_01 = posQ.xyxy + adjOffset.xxyx; //uint4(0, 0, 1, 0);
    uint4 posQ_23 = posQ.xyxy + adjOffset.xyyy; //uint4(0, 1, 1, 1);
    uint2 posQ_zz = posQ.zz   + adjOffset.xy;   //uint2(0, 1);

    uint scaleShift = dagScale - 2;
    uint4 nodeIndex4_0 = 0;
    uint4 nodeIndex4_1 = 0;
    uint4 shadowbit4_0 = 0;
    uint4 shadowbit4_1 = 0;

    bool4 intersected4_0 = true;
    bool4 intersected4_1 = true;

    for (; scaleShift > 1 && any(intersected4_0 || intersected4_1); --scaleShift)
    {
        uint4 vxsmAccess4_0 = vxsmOffset + nodeIndex4_0;
        uint4 vxsmAccess4_1 = vxsmOffset + nodeIndex4_1;

        // calculate where to go to child
        uint4 childDet_01 = ((posQ_01 >> scaleShift) & 0x00000002) << uint4(0, 1, 0, 1);
        uint4 childDet_23 = ((posQ_23 >> scaleShift) & 0x00000002) << uint4(0, 1, 0, 1);
        uint2 childDet_zz = ((posQ_zz >> scaleShift) & 0x00000002) << 2;
        uint4 cellShift4_0 = SumDet(childDet_01, childDet_23);
        uint4 cellShift4_1 = cellShift4_0;
        cellShift4_0 += childDet_zz.x;
        cellShift4_1 += childDet_zz.y;
        uint4 cellbit4_0 = 0x00000003 << cellShift4_0;
        uint4 cellbit4_1 = 0x00000003 << cellShift4_1;

        // calculate bit
        uint4 childmask4_0 = AccessVxShadowMaps(vxsmAccess4_0) >> 16;
        uint4 childmask4_1 = AccessVxShadowMaps(vxsmAccess4_1) >> 16;
        shadowbit4_0 = intersected4_0 ? (childmask4_0 & cellbit4_0) >> cellShift4_0 : shadowbit4_0;
        shadowbit4_1 = intersected4_1 ? (childmask4_1 & cellbit4_1) >> cellShift4_1 : shadowbit4_1;

        // if it has lit and shadowed, it is not decided yet(need to traverse more)
        intersected4_0 = shadowbit4_0 == 0x00000003;
        intersected4_1 = shadowbit4_1 == 0x00000003;

        // find next child node
        uint4 mask4_0 = ~(0xFFFFFFFF << cellShift4_0);
        uint4 mask4_1 = ~(0xFFFFFFFF << cellShift4_1);
        uint4 childrenbit4_0 = childmask4_0 & ((childmask4_0 & 0x0000AAAA) >> 1);
        uint4 childrenbit4_1 = childmask4_1 & ((childmask4_1 & 0x0000AAAA) >> 1);
        uint4 childIndex4_0 = countBits(childrenbit4_0 & mask4_0) + 1;
        uint4 childIndex4_1 = countBits(childrenbit4_1 & mask4_1) + 1;

        // go down to the next node
        vxsmAccess4_0 += childIndex4_0;
        vxsmAccess4_1 += childIndex4_1;
        nodeIndex4_0 = intersected4_0 ? AccessVxShadowMaps(vxsmAccess4_0) : nodeIndex4_0;
        nodeIndex4_1 = intersected4_1 ? AccessVxShadowMaps(vxsmAccess4_1) : nodeIndex4_1;
    }

#ifdef VX_SHADOWS_DEBUG
    uint scale = scaleShift + 2;
    results[0] = nodeIndex4_0;
    results[1] = nodeIndex4_1;
    results[2] = shadowbit4_0;
    results[3] = shadowbit4_1;
#else
    results[0] = nodeIndex4_0;
    results[1] = nodeIndex4_1;
    results[2] = shadowbit4_0;
    results[3] = shadowbit4_1;
#endif
}

float TraverseNearestSampleVxShadowMap(uint begin, uint typeOffset, uint3 posQ, uint2 innerResult)
{
    uint vxsmOffset = begin + typeOffset;
    uint nodeIndex = innerResult.x;

    uint3 leaf = posQ % uint3(8, 8, 8);
    uint leafIndex = _VxShadowMapsBuffer[vxsmOffset + nodeIndex + leaf.z];
    if (leaf.y >= 4) leafIndex++;

    uint bitmask = _VxShadowMapsBuffer[vxsmOffset + leafIndex];

    uint maskShift = mad(leaf.y % 4, 8, leaf.x);
    uint mask = 0x00000001 << maskShift;

    float attenuation = (bitmask & mask) == 0 ? 1.0 : 0.0;

    return attenuation;
}

float TraverseBilinearSampleVxShadowMap(uint begin, uint typeOffset, uint3 posQ, uint4 innerResults[2], float2 lerpWeight)
{
    uint vxsmOffset = begin + typeOffset;
    uint4 nodeIndex4 = vxsmOffset + innerResults[0];

    uint4 posQ_x = posQ.xxxx + uint4(0, 1, 0, 1);
    uint4 posQ_y = posQ.yyyy + uint4(0, 0, 1, 1);

    uint4 leaf4_x = posQ_x % 8;
    uint4 leaf4_y = posQ_y % 8;
    uint  leaf4_z = posQ.z % 8;

    uint4 leafOffset = leaf4_y < 4 ? 0 : 1;
    uint4 leafIndex = vxsmOffset + uint4(
        _VxShadowMapsBuffer[nodeIndex4.x + leaf4_z],
        _VxShadowMapsBuffer[nodeIndex4.y + leaf4_z],
        _VxShadowMapsBuffer[nodeIndex4.z + leaf4_z],
        _VxShadowMapsBuffer[nodeIndex4.w + leaf4_z]) + leafOffset;

    uint4 bitmask4 = (innerResults[1] & VX_SHADOWS_LIT) ? 0x00000000 : 0xFFFFFFFF;

    if (innerResults[1].x == VX_SHADOWS_INTERSECTED) bitmask4.x = _VxShadowMapsBuffer[leafIndex.x];
    if (innerResults[1].y == VX_SHADOWS_INTERSECTED) bitmask4.y = _VxShadowMapsBuffer[leafIndex.y];
    if (innerResults[1].z == VX_SHADOWS_INTERSECTED) bitmask4.z = _VxShadowMapsBuffer[leafIndex.z];
    if (innerResults[1].w == VX_SHADOWS_INTERSECTED) bitmask4.w = _VxShadowMapsBuffer[leafIndex.w];

    uint4 maskShift4 = mad(leaf4_y % 4, 8, leaf4_x);
    uint4 mask4 = uint4(1, 1, 1, 1) << maskShift4;

    float4 attenuation4 = (bitmask4 & mask4) == 0 ? 1.0 : 0.0;
    attenuation4.xy = lerp(attenuation4.xz, attenuation4.yw, lerpWeight.x);
    attenuation4.x  = lerp(attenuation4.x,  attenuation4.y,  lerpWeight.y);

    return attenuation4.x;
}

float TravereTrilinearSampleVxShadowMap(uint begin, uint typeOffset, uint3 posQ, uint4 innerResults[4], float3 lerpWeight)
{
    uint vxsmOffset = begin + typeOffset;
    uint4 nodeIndex4_0 = vxsmOffset + innerResults[0];
    uint4 nodeIndex4_1 = vxsmOffset + innerResults[1];

    uint4 posQ_x = posQ.xxxx + uint4(0, 1, 0, 1);
    uint4 posQ_y = posQ.yyyy + uint4(0, 0, 1, 1);
    uint2 posQ_z = posQ.zz   + uint2(0, 1);

    uint4 leaf4_x = posQ_x % 8;
    uint4 leaf4_y = posQ_y % 8;
    uint2 leaf4_z = posQ_z % 8;

    uint4 leafOffset = leaf4_y < 4 ? 0 : 1;
    uint4 leafIndex_0 = vxsmOffset + uint4(
        _VxShadowMapsBuffer[nodeIndex4_0.x + leaf4_z.x],
        _VxShadowMapsBuffer[nodeIndex4_0.y + leaf4_z.x],
        _VxShadowMapsBuffer[nodeIndex4_0.z + leaf4_z.x],
        _VxShadowMapsBuffer[nodeIndex4_0.w + leaf4_z.x]) + leafOffset;
    uint4 leafIndex_1 = vxsmOffset + uint4(
        _VxShadowMapsBuffer[nodeIndex4_1.x + leaf4_z.y],
        _VxShadowMapsBuffer[nodeIndex4_1.y + leaf4_z.y],
        _VxShadowMapsBuffer[nodeIndex4_1.z + leaf4_z.y],
        _VxShadowMapsBuffer[nodeIndex4_1.w + leaf4_z.y]) + leafOffset;

    uint4 bitmask4_0 = (innerResults[2] & VX_SHADOWS_LIT) ? 0x00000000 : 0xFFFFFFFF;
    uint4 bitmask4_1 = (innerResults[3] & VX_SHADOWS_LIT) ? 0x00000000 : 0xFFFFFFFF;

    if (innerResults[2].x == VX_SHADOWS_INTERSECTED) bitmask4_0.x = _VxShadowMapsBuffer[leafIndex_0.x];
    if (innerResults[2].y == VX_SHADOWS_INTERSECTED) bitmask4_0.y = _VxShadowMapsBuffer[leafIndex_0.y];
    if (innerResults[2].z == VX_SHADOWS_INTERSECTED) bitmask4_0.z = _VxShadowMapsBuffer[leafIndex_0.z];
    if (innerResults[2].w == VX_SHADOWS_INTERSECTED) bitmask4_0.w = _VxShadowMapsBuffer[leafIndex_0.w];
    if (innerResults[3].x == VX_SHADOWS_INTERSECTED) bitmask4_1.x = _VxShadowMapsBuffer[leafIndex_1.x];
    if (innerResults[3].y == VX_SHADOWS_INTERSECTED) bitmask4_1.y = _VxShadowMapsBuffer[leafIndex_1.y];
    if (innerResults[3].z == VX_SHADOWS_INTERSECTED) bitmask4_1.z = _VxShadowMapsBuffer[leafIndex_1.z];
    if (innerResults[3].w == VX_SHADOWS_INTERSECTED) bitmask4_1.w = _VxShadowMapsBuffer[leafIndex_1.w];

    uint4 maskShift4 = mad(leaf4_y % 4, 8, leaf4_x);
    uint4 mask4 = uint4(1, 1, 1, 1) << maskShift4;

    float4 attenuation4_0 = (bitmask4_0 & mask4) == 0 ? 1.0 : 0.0;
    float4 attenuation4_1 = (bitmask4_1 & mask4) == 0 ? 1.0 : 0.0;
    attenuation4_0.xy = lerp(attenuation4_0.xz, attenuation4_0.yw, lerpWeight.x);
    attenuation4_0.x  = lerp(attenuation4_0.x,  attenuation4_0.y,  lerpWeight.y);
    attenuation4_1.xy = lerp(attenuation4_1.xz, attenuation4_1.yw, lerpWeight.x);
    attenuation4_1.x  = lerp(attenuation4_1.x,  attenuation4_1.y,  lerpWeight.y);

    return lerp(attenuation4_0.x, attenuation4_1.x, lerpWeight.z);
}

uint MaskBitsetVxShadowsType(uint vxShadowsBitset)
{
    return vxShadowsBitset & 0xC0000000;
}

uint MaskBitsetVxShadowMapBegin(uint vxShadowsBitset)
{
    return vxShadowsBitset & 0x3FFFFFFF;
}

bool IsVxShadowsEnabled(uint vxShadowsBitset)
{
    return MaskBitsetVxShadowsType(vxShadowsBitset) != 0x00000000;
}

bool IsVxShadowsDisabled(uint vxShadowsBitset)
{
    return MaskBitsetVxShadowsType(vxShadowsBitset) == 0x00000000;
}

bool IsVxShadowsOnly(uint vxShadowsBitset)
{
    return MaskBitsetVxShadowsType(vxShadowsBitset) == VX_SHADOWS_ONLY;
}

float NearestSampleVxShadowing(uint begin, float3 positionWS)
{
    uint dagScale = _VxShadowMapsBuffer[begin + 2];
    uint voxelResolution = 1 << dagScale;
    float4x4 worldToShadowMatrix =
    {
        asfloat(_VxShadowMapsBuffer[begin +  3]),
        asfloat(_VxShadowMapsBuffer[begin +  4]),
        asfloat(_VxShadowMapsBuffer[begin +  5]),
        asfloat(_VxShadowMapsBuffer[begin +  6]),

        asfloat(_VxShadowMapsBuffer[begin +  7]),
        asfloat(_VxShadowMapsBuffer[begin +  8]),
        asfloat(_VxShadowMapsBuffer[begin +  9]),
        asfloat(_VxShadowMapsBuffer[begin + 10]),

        asfloat(_VxShadowMapsBuffer[begin + 11]),
        asfloat(_VxShadowMapsBuffer[begin + 12]),
        asfloat(_VxShadowMapsBuffer[begin + 13]),
        asfloat(_VxShadowMapsBuffer[begin + 14]),

        0.0, 0.0, 0.0, 1.0,
    };

    float3 posNDC = mul(worldToShadowMatrix, float4(positionWS, 1.0)).xyz;
    float3 posP = posNDC * (float)voxelResolution;
    uint3  posQ = (uint3)posP;

    if (any(posQ >= (voxelResolution.xxx - 1)))
        return 1;

    uint2 result;
    TraverseVxShadowMapPosQ(begin, OFFSET_DIR, posQ, result);

    if (result.y != VX_SHADOWS_INTERSECTED)
        return (result.y & 0x000000001) ? 1 : 0;

    float attenuation = TraverseNearestSampleVxShadowMap(begin, OFFSET_DIR, posQ, result);

    return attenuation;
}

#endif // UNITY_VX_SHADOWMAPS_COMMON_INCLUDED
