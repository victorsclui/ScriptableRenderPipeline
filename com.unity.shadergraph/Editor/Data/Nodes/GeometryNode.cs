using System;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    abstract class GeometryNode : AbstractMaterialNode
    {
        public virtual string[] spaceEntries => new string[] {"Object", "View", "World", "Tangent"};
        public virtual int defaultEntry => 2;

        [SerializeField]
        private PopupList m_SpacePopup;        

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
            }
        }
        public CoordinateSpace space => (CoordinateSpace)m_SpacePopup.selectedEntry;

        public GeometryNode()
        {
            m_SpacePopup = new PopupList(spaceEntries, defaultEntry);  
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
