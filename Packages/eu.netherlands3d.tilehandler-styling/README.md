TileHandler Styling
===================

This is an add-on for the TileHandler that adds shaders, textures and settings to style your output. This package
is not included by default to allow developers to add their own styling, unless they want to try this out first or 
use it as a basis for their own styling.

Setting it up
-------------

To get this to work, you need to do a couple of things:

1. Replace your `Directional Light` with the `Sun` prefab
2. Add the `Material Library` and `EnvironmentSettings` prefabs to your scene
3. In the instantiated `EnvironmentSetting`: link the `Sun` object to the `Directional Light Sun` field
4. In the TileHandler's `Terrain`, `Building` and `Trees` child: load the BinaryMeshLayer presets included in this 
   package.
5. In the Lighting window:
   1. Set the `Lighting Settings` Asset to the included `MainLighting`
   2. In Environment: Set the skybox material to `Skybox_Textured_Simple` and Environment Lighting to the 
      type `Skybox` with an intensity of `1.5`.
   3. In Environment: set a custom Environment Reflection with the Cubemap `Reflection_Cubemap`
   4. Enable fog with color `#F0EDE4` in `Linear` mode with a start of `0` and end of `20000`

With all these settings, your TileHandler output should look a lot better immediately. You may want to tweak the LOD 
distances of the TileHandler layers and the clipping distance of the camera for the best results. 
