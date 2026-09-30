# LAS point-cloud memory audit

Audit target: `feature/REALLOCATE-pre-oplevering`, Unity `6000.3.10f1`.

This document describes the implementation as inspected and extended on 2026-09-30. It separates the original behavior from the bounded WebGL LAS streaming path implemented during this work and from the remaining renderer and persistence work.

## Executive diagnosis

The LAS parser itself does **not** read the complete file into a managed C# `byte[]`. It opens a `FileStream`, reads the header and VLRs, and seeks directly to sampled point records. The default retained-point limit is 2,000,000.

The two dominant memory costs in the original implementation were elsewhere:

1. WebGL file import read the complete browser `File` with `FileReader.readAsArrayBuffer`, wrote a single full-size `Uint8Array` record to IndexedDB, and then mirrored that record into Emscripten IDBFS/MEMFS. Before this audit, `window.filereader` also retained the full `ArrayBuffer` after the write. Newly selected `.las` files now bypass that path and use bounded `File.slice` range reads; other file types retain the original behavior.
2. Rendering expands every rendered point into a four-vertex/six-index billboard. Staging lists, the readable Unity `Mesh`, and the GPU copy coexist. This is approximately 336 bytes per rendered point in addition to the retained 20-byte `RenderPoint`.

There is a load/resident-point budget, per-cell LOD, frustum culling, and a 75 m spatial grid already. There is no global per-frame render budget: every visible cell independently receives up to its per-mesh limit, and meshes built for cells that later become invisible remain resident.

## 1. Existing end-to-end path

```text
ImportAssetPanel
  -> FileOpen (marks .las as streamed on WebGL)
  -> browser <input type=file>
  -> browser-file:// opaque source URI
  -> FileTypeAdapter / LASImportAdapter
  -> LASPointCloudPreset / LASPointCloudPropertyData
  -> LASPointCloudLayer
  -> WebGLBrowserFileLASByteSource
  -> File.slice ranges (4 MiB maximum point-data request)
  -> LASStreamingReader (bounded header/VLR/record blocks)
  -> sampled LASPointData values
  -> coordinate conversion / local anchoring
  -> 75 m PointCloudChunk grid
  -> List<RenderPoint>
  -> four staging Lists per visible chunk
  -> readable Unity Mesh (triangle billboards)
  -> Unity graphics upload
  -> PointCloudVertexColor shader
  -> GPU
```

### Selection and persistence

- `Assets/UI Toolkit/Scripts/Panels/ImportAssetPanel.cs`
  - `ImportAssetPanel.OnUploadStarted` requests the existing `FileOpen` service with `las` among the accepted extensions.
- `Packages/eu.netherlands3d.filebrowser/Runtime/Scripts/FileOpen.cs`
  - Editor/standalone: copies the chosen file to `Application.persistentDataPath`.
  - WebGL: creates an HTML input and delegates selection callbacks to `FileInputIndexedDB`; LAS selections are tagged for direct streaming.
- `Packages/eu.netherlands3d.filebrowser/Runtime/Plugins/webgl/StandaloneFileBrowser.jslib`
  - Registers a selected LAS `File` behind an opaque `browser-file://` URI.
  - `BrowserFileReadRange` uses `File.slice` and copies only the requested range into WASM, where C# copies it into a reusable managed block and immediately frees the interop allocation.
  - Non-streamed file types still use `ReadFile`, one `FILE_DATA` record, and `FS.syncfs`.
- `Packages/eu.netherlands3d.filebrowser/Runtime/Scripts/FileInputIndexedDB.cs`
  - Waits for all callbacks. It skips IDBFS synchronization when the selection contains only direct browser sources.
- `Assets/Prefabs/FileOpener.prefab` and `Assets/Prefabs/UserFileUploads.prefab`
  - Serialized UnityEvents connect file selection to `FileTypeAdapter`.
- `Assets/_Application/DataTypeAdapters/FileTypeAdapter.cs`
  - Selects the adapter by extension and preserves opaque browser source URIs; persisted files remain rooted in `persistentDataPath`.
- `Assets/Scriptables/DataTypeImportAdapters/FileTypeAdapter.asset`
  - Registers `.las` with `LASImportAdapter`.

### Layer creation and project URI

- `Assets/_Functionalities/LASImporter/Scripts/LASImportAdapter.cs`
  - Reads only four bytes to validate local files. A direct browser source is validated by the streaming header parser before points are accepted.
- `Assets/_Application/Projects/AssetUriFactory.cs`
  - Converts the persistent file to a `project:///` URI and resolves it on reload.
- `Assets/_Functionalities/LASImporter/Scripts/LayerPresets/LASPointCloudPreset.cs`
  - Builds the LAS layer with the existing prefab identifier.
- `Assets/_Functionalities/LASImporter/Scripts/LASPointCloudPropertyData.cs`
  - Persists the LAS asset URI.
- `Assets/Prefabs/CustomLayers/LASPointCloudLayer.prefab`
  - Default budget: 2,000,000 retained points; grid: 75 m; requested per-cell mesh limit: 45,000 points.

### Header and point parsing

- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASFileReader.cs`
  - Holds a `FileStream` and `BinaryReader`.
  - Reads LAS 1.x header fields, legacy and extended point count, scale/offset, and bounds.
  - Scans projection VLRs for GeoTIFF keys, ASCII, or WKT; otherwise applies the Dutch RD bounds heuristic.
  - Reads X/Y/Z, classification, and RGB for formats 2, 3, 5, 7, 8, and 10.
  - Intensity is read but discarded. Return flags and other LAS attributes are also not retained. This is existing behavior and was not changed.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASByteSource.cs`
  - Defines the range-read ownership contract and implementations for a local `FileStream` and a WebGL browser `File`.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASStreamingReader.cs`
  - Parses a bounded header probe, projection VLRs, and point records from range buffers without materializing the source file.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASHeader.cs`
  - Stores header metadata and detected `CoordinateSystem`.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASPointData.cs`
  - Transient decoded value: three `double`s, classification, `Color32`, and `HasColor`.

`LASPointCloudLayer` calculates `stride = ceil(total point count / maxLoadedPoints)` and calls `TryReadPoint` at those record indices. Therefore source parsing is sparse rather than a sequential full decode when the file exceeds the point budget.

### Coordinates and retained representation

- `Assets/_Functionalities/LASImporter/Scripts/LASPointCloudLayer.cs`
  - Georeferenced data is converted through the Netherlands3D coordinate system and stored relative to a georeferenced anchor.
  - Data without a detected CRS is centered on LAS bounds and preserves the existing X/Z/Y Unity axis mapping.
  - Points are assigned to X/Z grid keys of `floor(position / 75 m)`.
  - `RenderPoint` retains `Vector3`, `Color32`, a color-presence `bool`, and classification `byte`.
  - The practical sequential struct size is approximately 20 bytes after alignment.

No LAS code creates a `NativeArray`, `ComputeBuffer`, `GraphicsBuffer`, per-point C# class, or managed whole-file `byte[]`.

### Mesh and rendering

- Nested `LASPointCloudLayer.PointCloudChunk`
  - Retains `List<RenderPoint>`.
  - Rebuilds four staging lists: `Vector3`, `Color32`, `Vector2` corner, and `int` index.
  - Expands one point to four identical positions, four colors, four corners, and six triangle indices.
  - Uses `IndexFormat.UInt16` on WebGL and caps each WebGL chunk to 16,000 points (64,000 vertices).
  - Calls `Mesh.SetVertices`, `SetColors`, `SetUVs`, and `SetIndices`.
  - Calls `MarkDynamic`, but does not call `UploadMeshData(true)` because LOD and color changes rebuild the same mesh.
- `Assets/_Functionalities/LASImporter/Shaders/PointCloudVertexColor.shader`
  - Expands billboard corners in clip space and clips fragments to a circle.
- `Assets/_Functionalities/LASImporter/Materials/PointCloudVertexColor.mat`
  - Supplies point-size defaults.
- `Assets/_Functionalities/LASImporter/Scripts/LASPointCloudRenderPropertyData.cs`
  - Stores color mode, point sizing, load budget, per-cell limit, and distance multiplier.
- `Assets/_Functionalities/LASImporter/Scripts/LASPointCloudRenderPropertySection.cs`
  - Existing UI for point size, reference distance, color mode, and retained-point budget.
- `Assets/_Functionalities/LASImporter/Scripts/LASClassificationColorPropertyData.cs`
  - Retains observed classification counts and styling rules.

## 2. Copies and bytes per point

### Source-file copies on the original WebGL path

At different moments the same source can exist as:

1. Browser `File`/`Blob` (its backing is browser/OS dependent and is not necessarily fully resident).
2. `FileReader.result` whole-file `ArrayBuffer`: `F` bytes.
3. `Uint8Array`: a view over that buffer, not another copy by itself.
4. IndexedDB structured-clone/write data: up to another `F` bytes transiently; persistent storage is not necessarily resident RAM.
5. The object returned while IDBFS syncs and the MEMFS/IDBFS file contents: approximately another `F` bytes in browser/Emscripten-side memory.
6. Small native/managed read buffers used by `FileStream`/`BinaryReader`; no full-size managed `byte[]` was found.

Before this audit, the global `window.filereader` kept item 2 alive into rendering. The first-pass fix scopes the reader locally and clears its handlers after completion. Whole-file read and whole-record IndexedDB persistence still remain and still set the upload-time lower bound.

### Retained and rendered point costs

Nominal costs from the actual fields and mesh layout:

| Allocation | Bytes per point | Notes |
|---|---:|---|
| `List<RenderPoint>` item | ~20 | `Vector3` 12 + `Color32` 4 + `bool` 1 + class 1 + alignment |
| Staging lists | 120 | 4 x (`Vector3` 12 + `Color32` 4 + `Vector2` 8) + 6 x `int` 4 |
| Readable Mesh CPU buffers | 108 | 4 x 24-byte vertex + 6 x 2-byte WebGL index |
| GPU vertex/index buffers | 108 | same packed vertex/index payload estimate |
| Total when retained and meshed | **356** | 20 + 120 + 108 + 108 |

`List<T>.Capacity` is often above `Count`, so real retained/staging capacity can approach the next geometric capacity and briefly coexist with the old backing array during growth. Unity-internal mesh bookkeeping, GameObjects, renderers, dictionaries, and allocator overhead are not included.

Useful formula for the current architecture:

```text
Unity + GPU LAS payload ~= 20 * loadedPoints + 336 * meshedPoints
```

The default budget caps `loadedPoints` at 2,000,000, but `meshedPoints` is not globally capped. If all retained points have acquired meshes, the nominal LAS payload is about 679 MiB, including about 206 MiB of GPU memory.

## 3. Representative peak estimates

Assumptions for a comparable estimate:

- LAS point format 3 / 34-byte records.
- Default 2,000,000 retained points.
- Worst current viewing history: all retained points have mesh buffers.
- Two to three simultaneous source-size buffers/copies around the WebGL import/sync/render handoff.
- Unity player baseline, scene/assets, browser overhead, IndexedDB implementation overhead, and `List<T>` spare capacity are excluded.

| Dataset | LAS records only | Estimated pre-fix peak |
|---:|---:|---:|
| 10 million | 324 MiB | **1.30-1.61 GiB** |
| 25 million | 811 MiB | **2.25-3.04 GiB** |
| 50 million | 1,621 MiB | **3.83-5.41 GiB** |

Actual LAS file size is `offsetToPointData + pointCount * pointRecordLength + trailing data`; substitute the header's real record length. The 2 GiB configured value is the maximum contiguous Unity WASM heap, not a budget for the whole browser tab or GPU. A browser can fail before the nominal maximum because JS buffers, WASM, engine assets, and GPU resources coexist.

After the first-pass reader-reference fix, the full `FileReader.result` no longer overlaps the later rendering lifetime. Newly selected LAS files now avoid this path entirely. A range request has a 4 MiB managed block plus a similarly sized `ArrayBuffer` and temporary WASM allocation, so the principal interop copies are bounded to roughly 12 MiB rather than scaling with `F`.

This bounded source is session-scoped. The browser `File` registry does not survive a page refresh. Project save also has a separate large-file hazard: `ProjectDataHandler.WriteProjectAssetToZipFile` currently calls `File.ReadAllBytes`, and a `browser-file://` asset is deliberately not embedded. Chunked project persistence is therefore still required before direct-streamed LAS layers can be reopened after a browser restart.

## 4. Ranked causes

1. **Whole-file WebGL import and IDBFS mirroring.** This scales with source size even though the LAS layer retains at most two million sampled points. It becomes the largest offender for very large files.
2. **Billboard mesh amplification and CPU/GPU duplication.** A 20-byte retained point becomes roughly 336 additional bytes once meshed. Meshes stay readable and staging arrays stay allocated.
3. **No global visible/resident GPU budget.** The existing budget controls decoded/retained points, not total mesh points rendered this frame or retained on the GPU. Invisible chunks keep their mesh allocations.
4. **Repeated whole-chunk rebuilds.** During progressive load, visible dirty chunks rebuild from their beginning. Classification count synchronization previously caused additional full rebuilds every 50,000 decoded points.
5. **Geometric `List<T>` growth and spare capacities.** Four mesh staging lists plus point lists can temporarily hold old and new arrays during resizing. This matters on WebGL because GC opportunities are frame-boundary constrained.
6. **Per-record seek/managed parser overhead.** The active load path seeks once per sampled point and decodes in IL2CPP-generated WebAssembly on the main thread. This is primarily a throughput issue, not the dominant retained-memory cost.

## 5. Existing render budget and spatial behavior

The current implementation already has useful pieces:

- A configurable **load/RAM point budget** (`MaxLoadedPoints`, default 2 million).
- A 75 m X/Z spatial grid.
- Camera-frustum activation by chunk.
- Explicit priority for the camera's current X/Z cell: it remains active even when its tight point bounds fall outside the near-plane test, receives the close-chunk point allowance, and uses the minimum LOD stride.
- Distance-based sampling stride per chunk.
- A per-mesh cap (16,000 points on WebGL due to four vertices per point and UInt16 indices).

It does not separate all four required quantities:

| Quantity | Current control |
|---|---|
| Points in source | LAS header count |
| Points retained in RAM | `MaxLoadedPoints` |
| Points resident in GPU meshes | No global cap; prior meshes persist |
| Points rendered this frame | Sum of independently visible chunk meshes; no global cap |

A first global target of 2-5 million visible points is too high for the present 108-byte GPU billboard representation (about 206-515 MiB GPU payload alone). Start with 1-2 million for the triangle-billboard fallback, then raise the configurable target only after a one-vertex/packed renderer is validated.

## 6. Comparison with lasviewer.github.io

The reference viewer's `floader.js` currently starts with a 1 MiB chunk, calls `File.slice(offset, offset + length)`, reads only that blob, allocates an equal-sized WASM block, copies it into `HEAPU8`, calls native `LdLasCppJS`, and immediately `_free`s the input block. The native callback can return the next requested chunk size. Its published site points to the `ezpoint3d` C++ source.

| Concern | Current NL3D | Reference viewer |
|---|---|---|
| Browser source read | Whole file | `File.slice` bounded chunks |
| Browser-to-WASM copy | Indirect through whole IndexedDB/IDBFS file | One chunk into `_malloc` block |
| Decode | C# `BinaryReader`, compiled by IL2CPP to WASM in WebGL | Native C/C++ WASM callback |
| Temporary input lifetime | Whole browser/virtual-FS source persists | Input block `_free` after callback |
| Retained points | 20-byte CPU struct, capped by load budget | Native compact structures (implementation-specific) |
| Rendering | Four-vertex Unity Mesh billboard | Custom native/WebGL renderer |
| Spatial behavior | 75 m grid + per-cell LOD | Native viewer-specific structures |
| Unity integration | Preserves layer UI, styling, coordinates, project assets | Standalone application |

The architecture can be borrowed without copying the standalone renderer: keep NL3D layer/property/georeferencing behavior, replace only the byte source and incremental parser first, then address renderer storage behind the existing layer API.

## 7. WebGL settings and constraints

`ProjectSettings/ProjectSettings.asset` contains:

- Initial WASM heap: 256 MiB.
- Maximum WASM heap: 2,048 MiB.
- Growth mode value `2`, Unity's geometric mode.
- Geometric step: 20%, capped at 96 MiB per growth.
- WebGL threads: disabled.
- No custom Emscripten arguments and no custom `malloc`/`realloc` implementation were found.

The branch name is not reflected in custom reallocation code. `git blame` shows the growth settings predate this LAS branch. Geometric WASM growth avoids a fixed 256 MiB ceiling but does not solve retained data, fragmentation, JS-side buffers, or GPU allocations. Heap growth can fail when the browser cannot extend/reserve the required contiguous WASM memory.

Unity-specific constraints that prevent a direct match with a standalone C++ viewer:

- C# and Unity engine state share the Unity WASM heap and engine lifecycle.
- Browser file access must be asynchronous and cannot expose an ordinary OS path.
- Project persistence currently depends on IDBFS holding the whole uploaded asset.
- C# WebGL work is main-threaded in this configuration.
- `ComputeBuffer`/`StructuredBuffer` rendering requires shader/platform capabilities beyond the current shader target 3.0 and must not replace the mesh path without an actual WebGL build test.
- `Mesh.UploadMeshData(true)` makes a mesh non-readable, while current styling and LOD rebuild meshes in place.
- GPU driver allocations are outside Unity's managed accounting and browser limits vary by device/browser.

## 8. Reversible implementation plan

### Step 1 - remove avoidable retention and rebuilds (implemented)

Files:

- `StandaloneFileBrowser.jslib`
- `LASFileReader.cs`
- `LASPointCloudLayer.cs`

Changes:

- Do not retain `FileReader.result` globally.
- Do not rebuild all visible meshes when only classification counts change.
- Make the public stride iterator seek directly to sampled records instead of decoding skipped records.
- Add Editor/development-build telemetry.

Expected reduction: one source-file-sized long-lived JS reference after upload; avoids repeated mesh upload/transient pressure during progressive load. Risk: low. UI, coordinates, styling, and Editor behavior remain unchanged.

### Step 2 - introduce a byte-source abstraction (implemented for newly selected WebGL LAS)

Files/classes:

- Refactor `LASFileReader` behind `ILASByteSource` with seek/range reads.
- Add local `FileLASByteSource` for Editor/standalone.
- Add WebGL source registration and range request methods in `StandaloneFileBrowser.jslib` and a small C# bridge.
- Extend the LAS import metadata/property with an opaque browser source ID while preserving `project:///` compatibility.

Use a default **4 MiB** payload chunk, aligned to complete point records and configurable from 1-16 MiB. Four MiB matches roughly five current 25,000-point frames for a 34-byte record and bounds two or three simultaneous interop copies to roughly 8-12 MiB. Read the fixed header first; request the exact VLR/header-to-point-data range separately with a sanity cap.

Implemented reduction: replaces `F`-sized LAS input buffers with `O(4 MiB)` temporary buffers. Editor and standalone retain the established `LASFileReader` path; WebGL uses browser ranges. The remaining medium-risk item is durable project persistence.

Ownership rule:

1. Browser registry owns the `File` or chunked persistent source.
2. Unity owns a fixed unmanaged/native destination buffer for one outstanding request.
3. JS writes no more than the requested capacity and signals completion by request ID.
4. Parser consumes records, retaining any sub-record tail only.
5. Unity releases/reuses its buffer; JS drops the chunk `ArrayBuffer` reference.
6. Cancellation/layer destruction invalidates request IDs before buffers are disposed.

For reloadable saved projects, store separate fixed-size IndexedDB records (or OPFS where supported), not one giant `contents` value. A session-only browser `File` handle is insufficient for project persistence.

### Step 3 - incremental point-record parser (implemented for the WebGL source)

Files:

- `LASFileReader.cs` split into header parser and record-block parser.
- `LASPointCloudLayer.cs` consumes decoded blocks over frames.

The WebGL loader aligns every request to complete records, decodes sequential blocks, retains only the points selected by the existing global stride, and reuses one managed point-data buffer. Header reads tolerate partial range completions. Regression tests cover partial reads, LAS 1.4 header values, invalid signatures, and format-3 XYZ/classification/RGB output. Editor/standalone deliberately keep their established sparse `FileStream` reader for lower rollout risk.

### Step 4 - enforce separate RAM/GPU/frame budgets

Files:

- `LASPointCloudRenderPropertyData.cs`
- `LASPointCloudLayer.cs`
- existing property UI if the additional settings are exposed.

Add configurable `MaxResidentGpuPoints` and `MaxVisiblePoints`. Rank visible cells by frustum, distance, and projected size; assign the global budget deterministically. Evict or release mesh data for long-inactive cells and rebuild from retained packed blocks when needed. Expected reduction: bounds mesh/GPU memory independent of loaded points. Risk: medium; visual density changes, UI remains compatible through safe defaults.

### Step 5 - pack retained points

The current dataset-level `header.HasRgb` can replace the per-point color-presence flag. Pack RGB and classification into one `uint` (24-bit RGB + 8-bit class) beside `Vector3`, producing a 16-byte retained point without dropping currently used attributes. Intensity is currently unused; if product requirements add it, store it as `ushort`, not `float`.

Expected reduction: retained point list from about 20 to 16 bytes per point (20%), plus better upload layout. Risk: medium-low after parser/color-mode tests.

### Step 6 - replace billboard storage behind a capability-tested renderer

Preferred WebGL experiment:

- A raw 16-byte vertex layout (`float3 position + packed color`) and one vertex per point.
- `MeshTopology.Points`/point-size shader if the target browsers produce the required circular point size consistently.
- Keep the triangle-billboard renderer as fallback.

Do not assume `GraphicsBuffer`/`ComputeBuffer` procedural rendering works in the current WebGL shader model. Validate a minimal Unity WebGL build first. A successful one-vertex renderer changes GPU payload from roughly 108 bytes to roughly 16 bytes per point and removes billboard indices. Risk: high because point-size behavior and Unity WebGL backend support are platform-dependent.

### Step 7 - hundreds-of-millions stage

Do not retain a uniform sample of the entire dataset. During streaming, bin accepted points into persistent spatial cells with multiple deterministic density levels. Keep cell metadata/bounds resident; page packed point blocks between IndexedDB/OPFS, RAM, and GPU. Each frame:

1. Frustum-test cells.
2. Estimate screen-space contribution.
3. Choose a density level.
4. Allocate the global visible-point budget nearest/most important first.
5. Asynchronously request missing cell blocks.
6. Evict least-recently-used GPU and RAM blocks independently.

COPC/Potree-compatible preprocessing can be a later ingestion option, but is not required for the first bounded-memory WebGL path.

## 9. Development instrumentation

`LASPointCloudLayer` now emits `[LAS memory]` logs only when `UNITY_EDITOR` or `DEVELOPMENT_BUILD` is defined. It logs:

- source file bytes;
- LAS point count, point-record size/format, sample stride, and load budget;
- decoded point count;
- number of spatial chunks;
- points represented by current meshes;
- estimated retained-point list capacity;
- mesh staging-list capacity;
- readable mesh CPU payload estimate;
- GPU payload estimate;
- mesh rebuild count;
- elapsed load time.

The estimates intentionally label browser FileReader/IndexedDB/MEMFS memory as external because Unity cannot measure it from this layer. Browser DevTools and a development WebGL build are needed to correlate total tab memory with these logs.

## 10. Changed files

- `Assets/UI Toolkit/Scripts/Panels/ImportAssetPanel.cs`
  - Marks LAS as a direct-streamed extension while preserving the existing accepted-file list and UI.
- `Packages/eu.netherlands3d.filebrowser/Runtime/Plugins/webgl/StandaloneFileBrowser.jslib`
  - Adds browser source registration and bounded range reads; scopes legacy `FileReader` instances for non-streamed formats.
- `Packages/eu.netherlands3d.filebrowser/Runtime/Scripts/{FileOpen,FileInputIndexedDB,DrawHTMLOverCanvas}.cs`
  - Carries the streamed-extension selection through the existing import UI and avoids unnecessary IDBFS synchronization.
- `Assets/_Application/DataTypeAdapters/FileTypeAdapter.cs`, `Assets/_BuildingBlocks/DataTypeAdapters/IDataTypeAdapter.cs`, `LASImportAdapter.cs`, and `LASPointCloudPreset.cs`
  - Preserve an opaque browser source URI through layer creation without changing existing project URIs.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASByteSource.cs`
  - Adds local and asynchronous WebGL range-source implementations with request cancellation and explicit interop-buffer release.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASStreamingReader.cs`
  - Adds bounded header, VLR, and point-record parsing.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASFileReader.cs`
  - Exposes file length for diagnostics and avoids decoding records skipped by `ReadPoints(stride)`.
- `Assets/_Functionalities/LASImporter/Scripts/LASPointCloudLayer.cs`
  - Streams browser-selected LAS point blocks into the existing placement, classification, chunk, LOD, and mesh pipeline; suppresses count-only rebuilds and adds development-only load/memory estimates.
- `Assets/_Application/Tests/Runtime/LASStreamingReaderTests.cs`
  - Adds parser and partial-read regression coverage.
- `Assets/_Functionalities/LASImporter/Scripts/Parsing/LASImporterAssemblyInfo.cs`
  - Exposes parser internals only to the existing Twin test assembly.
- `Documentation/LAS_POINT_CLOUD_MEMORY_AUDIT.md`
  - Records this audit, estimates, constraints, and staged architecture.

## Sources used for external comparison

- [lasviewer.github.io repository](https://github.com/lasviewer/lasviewer.github.io)
- [Reference `floader.js`](https://github.com/lasviewer/lasviewer.github.io/blob/main/floader.js)
- [Unity Web memory manual](https://docs.unity3d.com/Manual/webgl-memory.html)
- [Unity Web player settings](https://docs.unity3d.com/Manual/class-PlayerSettingsWebGL.html)
