# Mesh Clipper

The MeshClipper Package is a script that allows you to cut GameObject meshes into smaller pieces based on a specified Bounds object.
This package is designed to help you dynamically slice and modify meshes within your Unity project.

## Features

- Mesh Clipping: Cut GameObject meshes into smaller pieces.
- Bounds Input: Specify a Bounds object to define the area where the mesh should be cut.
- Clipped Mesh Output: Receive a clipped mesh as the output, providing you with a modified version of the original mesh.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.meshclipper
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

```cs
// Create a new MeshClipper
MeshClipper clipper = new MeshClipper(); 

// Tell the clipper which gameObject the target mesh is attached to
clipper.SetGameObject(targetGameObject);

// Clip the desired submesh using a Bounds object
var submeshIndex = 0;
clipper.ClipSubMesh(bounds, submeshIndex);

// After clipping the clipper contains a list of Vector3. Each set of 3 coordinates describes a triangle (orientation is counterClockwise) that we can use to spawn a new GameObject with a new mesh:
Mesh clippedMesh = new Mesh();
clippedMesh.vertices = vertices.ToArray();
clippedMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
clippedMesh.RecalculateNormals();

var clippedGameObject = new GameObject("Clipped");
clippedGameObject.AddComponent<MeshFilter>().sharedMesh = clippedMesh;
clippedGameObject.AddComponent<MeshRenderer>().material = clippedMeshMaterial;
```
