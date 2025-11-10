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
        private readonly Tile tile;

        // Static factory methods (root builders)
        public static TileBuilder QuadTree(BoundingVolume volume, TemplatedUri uri, float baseGeometricError)
        {
            var tile = new Tile(volume, baseGeometricError);
            // TODO: init quadtree
            return new TileBuilder(tile);
        }

        public static TileBuilder Octree(BoundingVolume volume, TemplatedUri uri, float baseGeometricError)
        {
            var tile = new Tile(volume, baseGeometricError);
            // TODO: init octree
            return new TileBuilder(tile);
        }

        public static TileBuilder Grid(BoundingVolume volume, TemplatedUri uri, float baseGeometricError)
        {
            var tile = new Tile(volume, baseGeometricError);
            // TODO: init grid
            return new TileBuilder(tile);
        }

        public static TileBuilder Explicit(BoundingVolume volume, float geometricError)
        {
            var tile = new Tile(volume, geometricError);
            return new TileBuilder(tile);
        }

        public TileBuilder(Tile tile)
        {
            this.tile = tile;
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

        public TileBuilder AddChild(TileBuilder child)
        {
            child.Parent(this);
            Children.Add(child);

            return this;
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
            return tile;
        }
    }
}