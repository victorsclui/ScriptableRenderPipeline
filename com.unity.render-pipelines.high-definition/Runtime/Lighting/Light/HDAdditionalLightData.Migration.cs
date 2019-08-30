using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDAdditionalLightData : ISerializationCallbackReceiver, IVersionable<HDAdditionalLightData.Version>
    {
        // TODO: Use proper migration toolkit

        enum Version
        {
            _Unused00,
            _Unused01,
            ShadowNearPlane,
            LightLayer,
            ShadowLayer,
            Last,
        }

        Version IVersionable<Version>.version
        {
            get => m_Version;
            set => m_Version = value;
        }

        [SerializeField]
        private Version m_Version = Version.Last;

        private static readonly MigrationDescription<Version, HDAdditionalLightData> k_HDLightMigrationSteps
            = MigrationDescription.New(
                MigrationStep.New(Version.ShadowNearPlane, (HDAdditionalLightData t) =>
                {
                    // Added ShadowNearPlane to HDRP additional light data, we don't use Light.shadowNearPlane anymore
                    // ShadowNearPlane have been move to HDRP as default legacy unity clamp it to 0.1 and we need to be able to go below that
                    t.shadowNearPlane = t.legacyLight.shadowNearPlane;
                }),
                MigrationStep.New(Version.LightLayer, (HDAdditionalLightData t) =>
                {
                    // Migrate HDAdditionalLightData.lightLayer to Light.renderingLayerMask
                    t.legacyLight.renderingLayerMask = LightLayerToRenderingLayerMask((int)t.m_LightLayers, t.legacyLight.renderingLayerMask);
                }),
                MigrationStep.New(Version.ShadowLayer, (HDAdditionalLightData t) =>
                {
                    // Added the ShadowLayer
                    // When we upgrade the option to decouple light and shadow layers will be disabled
                    // so we can sync the shadow layer mask (from the legacyLight) and the new light layer mask
                    t.lightlayersMask = (LightLayerEnum)RenderingLayerMaskToLightLayer(t.legacyLight.renderingLayerMask);
                })
            );

        void ISerializationCallbackReceiver.OnAfterDeserialize() {}

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UpdateBounds();
        }

        void OnEnable()
        {
            if (shadowUpdateMode == ShadowUpdateMode.OnEnable)
                m_ShadowMapRenderedSinceLastRequest = false;
        }

        void Awake() => k_HDLightMigrationSteps.Migrate(this);

        #region Obsolete fields
        // To be able to have correct default values for our lights and to also control the conversion of intensity from the light editor (so it is compatible with GI)
        // we add intensity (for each type of light we want to manage).
        [Obsolete("Use Light.renderingLayerMask instead")]
        [FormerlySerializedAs("lightLayers")]
        LightLayerEnum m_LightLayers = LightLayerEnum.LightLayerDefault;
        #endregion
    }
}
