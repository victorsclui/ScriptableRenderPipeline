using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.WorldPosNode")]
    [Title("Input", "Geometry", "Position")]
    class PositionNode : GeometryNode, IMayRequirePosition
    {
        [SerializeField]
        private PopupList m_SpacePopup = new PopupList(new string[] {"Custom", "Override", "Space", "Popup"}, 2);

        [PopupControl("Space")]
        public override PopupList spacePopup 
        {
            get { return m_SpacePopup; }
            set
            {
                if (m_SpacePopup.selectedEntry == value.selectedEntry)
                    return;

                switch (m_SpacePopup.selectedEntry)
                {
                    case 0:
                        space = CoordinateSpace.Object;
                        break;
                    case 1:
                        space = CoordinateSpace.View;
                        break;
                    case 2:
                        space = CoordinateSpace.World;
                        break;  
                    case 3:
                        space = CoordinateSpace.Tangent;
                        break;
                }

                Dirty(ModificationScope.Graph);
                m_SpacePopup.selectedEntry = value.selectedEntry;
            }
        }

        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";


        public PositionNode()
        {
            name = "Position";
            precision = Precision.Float;
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    kOutputSlotName,
                    kOutputSlotName,
                    SlotType.Output,
                    Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", space.ToVariableName(InterpolatorType.Position));
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
