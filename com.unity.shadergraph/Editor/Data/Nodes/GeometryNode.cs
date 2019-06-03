using System;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    abstract class GeometryNode : AbstractMaterialNode
    {
        public virtual List<CoordinateSpace> validSpaces => new List<CoordinateSpace> {CoordinateSpace.Object, CoordinateSpace.View, CoordinateSpace.World, CoordinateSpace.Tangent};
        public virtual int defaultEntry => 2;
        private PopupList m_SpacePopup;

        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;

        [PopupControl("Space")]
        public PopupList spacePopup 
        {
            get { return m_SpacePopup; }
            set
            {
                if (m_SpacePopup.selectedEntry == value.selectedEntry)
                    return;

                Dirty(ModificationScope.Graph);
                m_SpacePopup.selectedEntry = value.selectedEntry;
                m_Space = (CoordinateSpace)m_SpacePopup.selectedEntry;
            }
        }
        public CoordinateSpace space => m_Space;

        public GeometryNode()
        {
            var names = validSpaces.Select(cs => cs.ToString()).ToArray();
            m_SpacePopup = new PopupList(names, defaultEntry);
        }
        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }
    }
}
