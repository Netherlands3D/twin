# Object Coloring

Package get acces to subobjects in unity Meshes.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add netherlands3d.Subobjects
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

### Adding ObjectMapping
add an objectMapping-class to a gameObject with a mesh. 
for each part in the mesh that you want to uniquely identify add an ObjectMappingItem to the items-list, containing the unique Identifier, the first index of the first vertex associated with the item and the number of vertices associated with the item.

### Override the color
- Create color maps as Dictionaries<string, Color>. add a prioritization index and add them to the GeometryColorizer.
the vertexcolors of the meshes will be adjusted appropriately.


