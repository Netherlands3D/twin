using System.Runtime.Serialization;
using JetBrains.Annotations;
using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "None")]
    public struct None : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.None;

        [CanBeNull] public TemplatedUri Subtrees { get; }

        public None(TemplatedUri subtrees)
        {
            Subtrees = subtrees;
        }
    }
}