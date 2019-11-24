using System;
using UnityEngine.Rendering.HighDefinition.Attributes;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Engine lighting property.</summary>
    [Flags]
    public enum LightingProperty
    {
        None = 0,

        /// <summary>Render only direct diffuse.</summary>
        DirectDiffuseOnly = 1 << 0,

        /// <summary>Render only direct specular.</summary>
        DirectSpecularOnly = 1 << 1,

        /// <summary>Render only indirect diffuse.</summary>
        IndirectDiffuseOnly = 1 << 2,

        /// <summary>Render only reflection.</summary>
        ReflectionOnly = 1 << 3,

        /// <summary>Render only refraction.</summary>
        RefractionOnly = 1 << 4,

        /// <summary>Represent how much final diffuse is from refraction and how much from other diffuse lighting.</summary>
        Transmittance = 1 << 5,

        /// <summary>Render only emissive.</summary>
        EmissiveOnly = 1 << 6,
    }

    /// <summary>Output a specific debug mode.</summary>
    public enum DebugFullScreen
    {
        None,
        Depth,
        ScreenSpaceAmbientOcclusion,
        MotionVectors
    }

    /// <summary>Use this request to define how to render an AOV.</summary>
    public unsafe struct AOVRequest
    {
        /// <summary>Default settings.</summary>
        public static AOVRequest @default = new AOVRequest
        {
            m_MaterialProperty = MaterialSharedProperty.None,
            m_LightingProperty = LightingProperty.None,
            m_DebugFullScreen = DebugFullScreen.None,
            m_LightFilterProperty = DebugLightFilterMode.None
        };

        MaterialSharedProperty m_MaterialProperty;
        LightingProperty m_LightingProperty;
        DebugLightFilterMode m_LightFilterProperty;
        DebugFullScreen m_DebugFullScreen;

        AOVRequest* thisPtr
        {
            get
            {
                fixed (AOVRequest* pThis = &this)
                    return pThis;
            }
        }

        /// <summary>Create a new instance by copying values from <paramref name="other"/>.</summary>
        /// <param name="other"></param>
        public AOVRequest(AOVRequest other)
        {
            m_MaterialProperty = other.m_MaterialProperty;
            m_LightingProperty = other.m_LightingProperty;
            m_DebugFullScreen = other.m_DebugFullScreen;
            m_LightFilterProperty = other.m_LightFilterProperty;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref AOVRequest SetFullscreenOutput(MaterialSharedProperty materialProperty)
        {
            m_MaterialProperty = materialProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref AOVRequest SetFullscreenOutput(LightingProperty lightingProperty)
        {
            m_LightingProperty = lightingProperty;
            return ref *thisPtr;
        }

        /// <summary>State the property to render. In case of several SetFullscreenOutput chained call, only last will be used.</summary>
        public ref AOVRequest SetFullscreenOutput(DebugFullScreen debugFullScreen)
        {
            m_DebugFullScreen = debugFullScreen;
            return ref *thisPtr;
        }

        /// <summary>Set the light filter to use.</summary>
        public ref AOVRequest SetLightFilter(DebugLightFilterMode filter)
        {
            m_LightFilterProperty = filter;
            return ref *thisPtr;
        }

        public void FillDebugData(DebugDisplaySettings debug)
        {
            debug.SetDebugViewCommonMaterialProperty(m_MaterialProperty);

            switch (m_LightingProperty)
            {
                case LightingProperty.DirectDiffuseOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.DirectDiffuse);
                    break;
                case LightingProperty.DirectSpecularOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.DirectSpecular);
                    break;
                case LightingProperty.IndirectDiffuseOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.IndirectDiffuse);
                    break;
                case LightingProperty.ReflectionOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.Reflection);
                    break;
                case LightingProperty.RefractionOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.Refraction);
                    break;
                case LightingProperty.Transmittance:
                    debug.SetDebugLightingMode(DebugLightingMode.Transmittance);
                    break;
                case LightingProperty.EmissiveOnly:
                    debug.SetDebugLightingMode(DebugLightingMode.Emissive);
                    break;
                default:
                {
                    debug.SetDebugLightingMode(DebugLightingMode.None);
                    break;
                }
            }

            debug.SetDebugLightFilterMode(m_LightFilterProperty);

            switch (m_DebugFullScreen)
            {
                case DebugFullScreen.None:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.None);
                    break;
                case DebugFullScreen.Depth:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.DepthPyramid);
                    break;
                case DebugFullScreen.ScreenSpaceAmbientOcclusion:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.SSAO);
                    break;
                case DebugFullScreen.MotionVectors:
                    debug.SetFullScreenDebugMode(FullScreenDebugMode.MotionVectors);
                    break;
                default:
                    throw new ArgumentException("Unknown DebugFullScreen");
            }
        }
    }
}

