using System;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.Tilekit.TileSets;

namespace Netherlands3D.Tilekit
{
    public class TileBuilder
    {
        private readonly List<TileBuilder> Children = new List<TileBuilder>();
        private string identifier;
        private TileBuilder parent;

        // Static factory methods (root builders)
        public static TileBuilder QuadTree(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init quadtree
            return new TileBuilder();
        }

        public static TileBuilder Octree(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init octree
            return new TileBuilder();
        }

        public static TileBuilder Grid(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init grid
            return new TileBuilder();
        }

        public static TileBuilder Explicit(BoundingVolume volume)
        {
            // TODO: init explicit structure
            return new TileBuilder();
        }

        // Builder chaining
        public TileBuilder Identifier(string identifier)
        {
            this.identifier = identifier;
            return this;
        }

        public TileBuilder Parent(TileBuilder tile)
        {
            this.parent = tile;
            return this;
        }

        public TileBuilder AddChild()
        {
            var child = new TileBuilder { parent = this };
            Children.Add(child);
            return child;
        }

        public TileContent AddContent()
        {
            // TODO: configure custom content
            throw new NotImplementedException();
        }

        public TileContent AddContent(TileSet tileSet)
        {
            // TODO: link external TileSet
            throw new NotImplementedException();
        }

        public TileContent AddContent(string contentType, Uri uri)
        {
            // TODO: configure remote content
            throw new NotImplementedException();
        }

        public Tile Build()
        {
            // TODO: create Tile and attach children
            throw new NotImplementedException();
        }
    }
}