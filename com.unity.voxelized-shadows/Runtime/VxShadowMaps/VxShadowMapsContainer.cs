using UnityEngine;

namespace UnityEngine.Experimental.VoxelizedShadows
{
    [ExecuteAlways]
    [AddComponentMenu("Rendering/VxShadowMaps/VxShadowMapsContainer", 100)]
    public class VxShadowMapsContainer : MonoBehaviour
    {
        public VxShadowMapsResources Resources = null;

        public void AssignResourcesToManager()
        {
            if (enabled && Resources != null)
                VxShadowMapsManager.Instance.LoadResources(Resources);
            else
                Debug.Log("Invalid Resources or VxShadowMapsContainer");
        }
    }
}
