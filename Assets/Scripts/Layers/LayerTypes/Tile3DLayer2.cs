namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayer2 : ReferencedLayer
    {
        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }
    }
}