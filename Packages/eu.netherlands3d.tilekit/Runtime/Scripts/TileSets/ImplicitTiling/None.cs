using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "None")]
    public class None : ImplicitTilingScheme
    {
        protected override SubdivisionScheme SubdivisionScheme { get; } = SubdivisionScheme.None;
    }
}