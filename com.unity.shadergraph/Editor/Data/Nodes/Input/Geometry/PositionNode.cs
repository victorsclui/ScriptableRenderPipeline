using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEngine.MaterialGraph.WorldPosNode")]
    [Title("Input", "Geometry", "Position")]
    class PositionNode : GeometryNode, IMayRequirePosition
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";
        public override string[] spaceEntries => new string[] {"Object", "View", "World", "Tangent", "Relative World"};

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
