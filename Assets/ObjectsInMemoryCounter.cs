using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tiles3D;
using UnityEngine;

public class ObjectsInMemoryCounter : MonoBehaviour
{

    public bool enable; 

    private static ObjectsInMemoryCounter _instance;
    private List<WeakReference<Tile>> _tiles = new List<WeakReference<Tile>>();

    public int tilesInMemory;


    [SerializeField]
    private List<Tile> debugTiles = new List<Tile>();



    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Multiple WeakReferenceCounters in scene!");
        }

        _instance = this;

        // Hook in op de callback van Tile
        Tile.OnTileCreated = Register;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Tile.OnTileCreated = null;
        }
    }

    private void Register(Tile tile)
    {
        if (enable)
        {
            _tiles.Add(new WeakReference<Tile>(tile));
        }
    }

    private void Update()
    {
        Cleanup();

        tilesInMemory = _tiles.Count(wr => wr.TryGetTarget(out _));

    }

    public void StoreFirstAliveTiles(int count)
    {
        debugTiles.Clear();

        foreach (var wr in _tiles)
        {
            if (wr.TryGetTarget(out var tile))
            {
                debugTiles.Add(tile);

                if (debugTiles.Count >= count)
                    break;
            }
        }

        Debug.Log($"Stored {debugTiles.Count} tiles in debugTiles list.");
    }
    public void DisposeUnDisposed()
    {
        foreach (var wr in _tiles)
        {
            if (wr.TryGetTarget(out var tile))
            {
                tile.Dispose();

            }
        }
    }

    private void Cleanup()
    {
        _tiles = _tiles
            .Where(wr => wr.TryGetTarget(out _))
            .ToList();
    }
}
