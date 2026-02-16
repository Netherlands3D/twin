using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class HideObjectPanel : FloatingPanel
    {
        private List<IMapping> mappings;

        public override void Initialize(Vector2 screenPosition, object context = null)
        {
            base.Initialize(screenPosition, context);

            mappings = context as List<IMapping>;
            if (mappings != null)
            {
                //HeaderText = $"Mappings ({mappings.Count})";

                contentContainer.Clear();

               //add mappings
            }
        }
    }
}
