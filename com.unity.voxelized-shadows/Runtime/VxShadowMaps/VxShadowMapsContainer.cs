using UnityEngine;

namespace UnityEngine.Experimental.VoxelizedShadows
{
    [ExecuteAlways]
    [AddComponentMenu("Rendering/VxShadowMaps/VxShadowMapsContainer", 100)]
    public class VxShadowMapsContainer : MonoBehaviour
    {
        public VxShadowMapsResources Resources = null;

        public void VerifyResources()
        {
            if (enabled && Resources != null)
                VxShadowMapsManager.Instance.LoadResources(Resources);
            else
                VxShadowMapsManager.Instance.UnloadResources();
        }
    }
}
