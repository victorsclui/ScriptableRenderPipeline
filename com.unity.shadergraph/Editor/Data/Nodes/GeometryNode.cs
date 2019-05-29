using System;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    abstract class GeometryNode : AbstractMaterialNode
    {
        [SerializeField]
        private PopupList m_SpacePopup = new PopupList(new string[] {"Object", "View", "World", "Tangent"}, 2);
        public CoordinateSpace space = CoordinateSpace.World;

        [PopupControl("Space")]
        public virtual PopupList spacePopup 
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
