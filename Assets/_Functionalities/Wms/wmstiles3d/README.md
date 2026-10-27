3DTiles
============

Using this package it is possible to load 3DTilesets in Unity3D at Runtime.

Requires a gameObject with SetGlobalRDOrigin.cs to create a connection between the real world and the Unity coordinateSystem.

see the SampleScene for how to set it up.

> Important: to be able to load google Earth 3DTiles, you need to install the package from https://github.com/Netherlands3D/Draco3DWebgl2022.git
> for working in a webgl-build, you should add the shadergraphs in packages/gltfast/runtime/shader to "Always include shaders"-list in the projectsettings.
