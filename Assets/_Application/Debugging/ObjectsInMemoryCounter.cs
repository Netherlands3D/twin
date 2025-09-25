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

    // Track Content objects as well
    private List<WeakReference<Content>> _contents = new List<WeakReference<Content>>();

    public int tilesInMemory;
    public int contentsInMemory;

    [SerializeField]
    private List<Tile> debugTiles = new List<Tile>();

    [SerializeField]
    private List<Content> debugContents = new List<Content>();



    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Multiple WeakReferenceCounters in scene!");
        }

        _instance = this;

        // Hook in op de callback van Tile en Content
        Tile.OnTileCreated = Register;
        Content.OnContentCreated = RegisterContent;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Tile.OnTileCreated = null;
            Content.OnContentCreated = null;
        }
    }

    private void Register(Tile tile)
    {
        if (enable)
        {
            _tiles.Add(new WeakReference<Tile>(tile));
        }
    }

    private void RegisterContent(Content content)
    {
        if (enable)
        {
            _contents.Add(new WeakReference<Content>(content));
        }
    }

    private void Update()
    {
        Cleanup();

        tilesInMemory = _tiles.Count(wr => wr.TryGetTarget(out _));
        contentsInMemory = _contents.Count(wr => wr.TryGetTarget(out _));

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
    public void StoreFirstAliveContents(int count)
    {
        debugContents.Clear();

        foreach (var wr in _contents)
        {
            if (wr.TryGetTarget(out var content))
            {
                debugContents.Add(content);

                if (debugContents.Count >= count)
                    break;
            }
        }

        Debug.Log($"Stored {debugContents.Count} contents in debugContents list.");
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

        // Also dispose alive Content objects
        foreach (var wr in _contents)
        {
            if (wr.TryGetTarget(out var content))
            {
                try
                {
                    content.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error disposing content: {ex.Message}");
                }
            }
        }
    }

    private void Cleanup()
    {
        _tiles = _tiles
            .Where(wr => wr.TryGetTarget(out _))
            .ToList();

        _contents = _contents
            .Where(wr => wr.TryGetTarget(out _))
            .ToList();
    }
}
