# Minimap

A minimap canvas solution to navigate through a WMTS map, and click to move the 3D camera to that location.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.minimap
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

Place the minimap prefab in a canvas.
Scriptable objects can be swapped/created to create new WMTS services.

## Events - WMTSMap

The `WMTSMap` class features two events: `onZoom` and `onClick`.

### onZoom

This event is triggered when the user performs a zoom action.

### onClick

This event is triggered when a user clicks on a location on the minimap. The event returns the coordinates of the
clicked location in the Rijksdriehoekformaat coordinate system.