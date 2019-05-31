using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.VoxelizedShadows
{
    [CustomEditor(typeof(VxShadowMapsContainer))]
    public class VxShadowMapsContainerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var container = target as VxShadowMapsContainer;

            if (container.Resources == null)
                return;

            float sizeInBytes = container.Resources.VxShadowMapList.Length * sizeof(uint);
            float sizeInMBytes = sizeInBytes / (1024.0f * 1024.0f);
            EditorGUILayout.LabelField("Total Size(MB): " + sizeInMBytes);

            foreach (var vxsData in container.Resources.VxShadowsDataList)
            {
                sizeInMBytes = (float)vxsData.SizeInBytes / (1024.0f * 1024.0f);

                string vxsmInfo0 = "Type: " + vxsData.Type.ToString();
                string vxsmInfo1 = "BeginOffset: " + vxsData.BeginOffset;
                string vxsmInfo2 = "Size(MB): " + sizeInMBytes;

                EditorGUILayout.LabelField(vxsmInfo0 + ", " + vxsmInfo1 + ", " + vxsmInfo2);
            }
        }

        [MenuItem("GameObject/Rendering/VxShadowMaps Container", priority = CoreUtils.gameObjectMenuPriority)]
        static void CreateVxShadowMapsContainer(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var containerGameObject = CoreEditorUtils.CreateGameObject(parent, "VxShadowMaps Container");
            GameObjectUtility.SetParentAndAlign(containerGameObject, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(containerGameObject, "Create " + containerGameObject.name);
            Selection.activeObject = containerGameObject;

            containerGameObject.AddComponent<VxShadowMapsContainer>();
        }
    }
}
