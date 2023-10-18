# CartesianTiles

Loads and unloads datasets in a cartesian coordinateSystem

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add netherlands3d.cartesiantiles
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage
 - create an empty GameObject and add TileHandler.cs to it.
 - for each dataset, add a Child-gameObject with a layerscript.
 - make sure netherlands3d.coordinates is set up(possibly using the CoordinateSetup.cs in the scene).

### TileHandler
Manages the loading and unloading if the tiles in the layers.
 - public void AddLayer(Layer layer); add the layer to the tilehandler while it is running
 - public void RemoveLayer(Layer layer); remove the layer from the tileHandler while it is running

### Layer
each layerType is derived from Layer.cs
through this class the following settings are available:
- bool isEnabled; if true, tiles are active and updated by the tilehandler. if false, tiles are set to Inactive and are NOT updated by the tilehander.
- bool pauseLoading; if true, Tiles remain in their current state, but are not updated by the tileHandler.
- int tileSize; the geometric size of the tiles for this layer.
- int layerPiority; the priorityLevel if this layer.
- list of Datasets.

### Dataset
settings:
- string description; only for recognizability in the editor.
- string geoLOD; only for  recognizability in the editor.
- strings path and pathQuery; are combined for creating the url at wich the tiledata can be found. use placeholders {x} and {y} for the X- and Y- coordinates in the url. (use file:// to load tiles from a local drive)
float maximumDistance; the maximum distance from the camera where the tiles can be loaded.

you can use multiple tilesets on a layer. order them with descending MaximumDistance. the tilehandler checks the distanceFromCamera of a new Tile and uses the settings where the tile falls within the maximumDistance (checking from the end of the list to the start of the list).
you can use this to load tiles at a farther distance with less detail.

### BinaryMeshLayer
settings for a collection of tiles, created with https://github.com/Amsterdam/CityDataToBinaryModel
settings:
DefaultMaterialList; list of materials to be added to each submesh of the newly loaded tile.
createMeshcollider(bool); if true, adds meshcollider to the tile when it is loaded.
tileShadowCastingMode; should the tiles cast shadows.
functions:
- public void AddMeshColliders(Vector3 onlyTileUnderPosition = default), Add meshcollider to all tiles. if a Vector3 is given, only the tile at that postition will have a meshcollider added.
- public void EnableShadows(bool enabled), enable or disable shadowcasting on the tiles.
