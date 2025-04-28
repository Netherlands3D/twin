using JetBrains.Annotations;
using KindMen.Uxios;
using Unity.Collections;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    public struct None : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.None;

        private NativeText subtrees;
        [CanBeNull] public TemplatedUri Subtrees => subtrees != null ? new TemplatedUri(subtrees.ConvertToString()) : null;

        public None(NativeText subtreesUri)
        {
            subtrees = subtreesUri;
        }
    }
}