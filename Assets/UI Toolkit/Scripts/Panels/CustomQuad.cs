using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class CustomQuad : VisualElement
    {
        public Vector2[] QuadVertices { get; set; } = new Vector2[4];

        [UxmlAttribute("color")] 
        public Color Color { get; set; } = Color.white;

        public CustomQuad()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            generateVisualContent += GenerateVisualContent;

            pickingMode = PickingMode.Ignore;
        }
        
        private void GenerateVisualContent(MeshGenerationContext mgc)
        {
            MeshWriteData mesh = mgc.Allocate(4, 6);

            for (int i = 0; i < 4; i++)
            {
                mesh.SetNextVertex(new Vertex
                {
                    position = QuadVertices[i],
                    tint = Color
                });
            }

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);

            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
            mesh.SetNextIndex(0);
        }

        public void Redraw()
        {
            MarkDirtyRepaint();
        }
    }
}