# Collada Mesh Export

This package provides scripts that allow you to export triangle lists or Unity GameObjects containing a mesh to a Collada file using streamwriting. This can be useful for creating 3D models that can be used in other applications or for sharing with others. With the ColladaFile class, you can easily create, fill, and export a Collada file from triangles. Additionally, the GameObjectToColladaFile script can retrieve the triangles from a GameObject Mesh (or nested meshes) and export them to a Collada file. 

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.collada
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

The following example shows how to export a custom triangle (made from 3 verts)

The GameObjectToColladaFile is a script that does this by retrieving the triangles from a GameObject Mesh ( or nested meshes )

Please note that currently the exported Collada is exported as triangles without 'sharing' any vertices, causing the exported mesh to have seperated triangles.

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
