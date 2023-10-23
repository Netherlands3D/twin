# Collada Mesh Export

This package contains scripts to export triangle lists or unity GameObjects containing a mesh to a Collada file using streamwriting.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add collada
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

To construct your own Collada mesh from triangles, use the ColladaFile class to create, fill and export a Collada file.

The GameObjectToColladaFile is a script that does this by retrieving the triangles from a GameObject Mesh ( or nested meshes )

Please note that the exported collada is exported as triangles without sharing any vertices.

```csharp
// Create a new collada file in memory that we will construct
 var collada = new ColladaFile();

// Add a mesh to the collada file by adding a list of vertices that make up the triangles
// A single vertex is defined as a double[] array
var vertexList = new List<double[]>();
vertexList.Add(new double[] { 0, 0, 0 });
vertexList.Add(new double[] { 1, 0, 0 });
vertexList.Add(new double[] { 1, 1, 0 });

collada.AddObjectTriangles(vertexList, "MyTriangle", material);

// Finish the document
collada.Finish();

// Save the collada document as a local file
collada.Save(filePath);
```


