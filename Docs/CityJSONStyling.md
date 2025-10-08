# CityJSON Styling Integration

## Summary

Recent changes ensure that CityJSON point and line visualizations honour layer styling in the same way as mesh-based geometries. The update touches the CityJSON import pipeline and the hierarchical layer styling helpers so that `BatchedMeshInstanceRenderer` components (`LineRenderer3D`, `PointRenderer3D`) receive color updates derived from the layer `Symbolizer`.

## Key Code Updates

- `Assets/_Functionalities/CityJSONImporter/Scripts/eu.netherlands3d.cityjson-mutations/Runtime/Scripts/Visualizer/CityObjectPointAndLineVisualizer.cs:17-38`  
  Automatically locates the batched renderer and emits `cityObjectVisualized` after feeding it coordinate collections, enabling consumers (e.g., the spawner) to react just like they do for mesh visualizers.

- `Assets/_Functionalities/CityJSONImporter/Scripts/CityJSONSpawner.cs:62-118`  
  Subscribes to every visualizer on a `CityObject`, adds mesh colliders only when appropriate, and invokes styling for either a classic `Renderer` or a `BatchedMeshInstanceRenderer`.

- `Assets/_Application/Layers/LayerTypes/HierarchicalObject/HierarchicalObjectLayerGameObject.cs:347-393`  
  Applies styling to all child `MeshRenderer` and batched renderers during `ApplyStyling`, exposing an overload that accepts `BatchedMeshInstanceRenderer` so the layer can treat points/lines as first-class features.

- `Assets/_Application/Layers/LayerTypes/HierarchicalObject/HierarchicalObjectTileLayerStyler.cs:51-145`  
  Extends the styler to branch per geometry type:  
  * Meshes remain untouched apart from material property blocks.  
  * `PointRenderer3D` now receives fill colors and refreshed instancing buffers.  
  * `LineRenderer3D` favours stroke colors while keeping point joints in sync.  
  * Fallback handling covers other batched renderers.  
  Materials are cloned on demand (named `" (Instance)"`) so project assets stay immutable.

## Styling System Overview

1. **Layer data & symbolizers** – `LayerGameObject` hosts `LayerData`, which exposes a default `Symbolizer`. The symbolizer stores properties such as fill/stroke color, mask bitmask, visibility, etc. (`Packages/eu.netherlands3d.layer-styles/Runtime/Scripts/Symbolizer.cs`).

2. **Feature generation** – Layers turn scene objects into `LayerFeature` instances (see `Assets/_Application/Layers/LayerGameObject.cs`). Each feature wraps the “geometry” reference (e.g., `MeshRenderer`, `LineRenderer3D`) plus an attribute map used for rule evaluation.

3. **Style resolution** – `StyleResolver` matches features against the layer’s styling rules, merging symbolizers so a final `Symbolizer` is produced per feature.

4. **Styling application** – Layer-specific stylers (like `HierarchicalObjectTileLayerStyler`) consume the resolved symbolizer. For mesh renderers, colors are pushed through material property blocks; for batched renderers we now ensure the material instance is unique and update instanced color arrays via `SetDefaultColors`.

5. **Masking & events** – `LayerGameObject` handles mask bitmasks and raises `OnStylingApplied` so other systems can respond to changes (e.g., UI updates, selection logic).

## Flow for CityJSON Point/Line Layers

1. `CityJSONSpawner` parses the file and attaches to every `CityObjectVisualizer`.
2. `CityObjectPointAndLineVisualizer` converts CityJSON boundaries into coordinate collections and feeds a `BatchedMeshInstanceRenderer`, then triggers `cityObjectVisualized`.
3. The spawner responds by calling `HierarchicalObjectLayerGameObject.ApplyStylingToRenderer`, passing either a `Renderer` or the batched renderer.
4. `HierarchicalObjectLayerGameObject` creates a `LayerFeature`, resolves its styling, and delegates to `HierarchicalObjectTileLayerStyler`, which now understands point and line renderers.
5. Materials are updated (cloned when necessary), colors set, instancing buffers refreshed, and mask bits applied. The user sees the chosen layer color reflected on the rendered point/line geometry.

## Considerations & Follow-up Ideas

- **Per-feature styling** – At present, batched renderers are recolored uniformly. Providing per-city-object color variance would require material property blocks or per-instance color buffers keyed by feature attributes.
- **Undo/redo** – Ensure that dynamic material instancing aligns with Unity’s undo system if editing within the editor.
- **Performance** – Batched renderer updates call `SetDefaultColors`, which currently rebuilds color arrays. If styling is toggled frequently, consider caching or partial updates.
- **Testing** – Manual verification remains essential: import CityJSON files containing `MultiPoint` and `MultiLineString` geometries, adjust fill/stroke colors in the layer UI, and confirm both segments and joints update.

This documentation should help future contributors understand both the architectural intent of the styling system and the specific changes that enabled CityJSON point and line coloring.
