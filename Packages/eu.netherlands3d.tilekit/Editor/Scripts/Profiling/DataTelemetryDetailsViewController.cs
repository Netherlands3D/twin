#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Tilekit.Profiling.Editor
{
    // NOTE: This struct must match your runtime emitted metadata row exactly.
    // If you renamed it in runtime, mirror that name/namespace here as well.
    // (Blittable, fixed buffer, same packing)
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TileSetMetaRow
    {
        public int DataSetId;
        public fixed byte NameUtf8[64];

        public long NativeReservedBytes;
        public long NativeUsedBytes;

        public int TilesAllocated;
        public int TilesActual;

        public int StringsAllocated;
        public int StringsActual;

        public int UrisAllocated;
        public int UrisActual;

        public int WarmCount;
        public int HotCount;
    }

    internal sealed class DataTelemetryDetailsViewController : ProfilerModuleViewController
    {
        // Must match your runtime Telemetry MetaId/MetaTag.
        static readonly Guid k_MetaId = new("B1B9A0E7-6A7B-4A21-9F64-0C8C70D46B2C");
        const int k_MetaTag = 1;

        // UI
        VisualElement? m_Root;
        MultiColumnListView? m_Table;
        Label? m_Header;

        // Data backing the table
        readonly List<RowVm> m_Rows = new(128);

        // Simple view-model so we don’t have to deal with fixed buffers in UI binding
        readonly struct RowVm
        {
            public readonly int Id;
            public readonly string Name;
            public readonly long Reserved;
            public readonly long Used;
            public readonly int TilesAllocated;
            public readonly int TilesActual;
            public readonly int StringsAllocated;
            public readonly int StringsActual;
            public readonly int UrisAllocated;
            public readonly int UrisActual;
            public readonly int Warm;
            public readonly int Hot;

            public RowVm(
                int id, 
                string name, 
                long reserved, 
                long used, 
                int tilesAllocated, 
                int tilesActual, 
                int stringsAllocated, 
                int stringsActual, 
                int urisAllocated, 
                int urisActual, 
                int warm, 
                int hot
            ) {
                Id = id;
                Name = name;
                Reserved = reserved;
                Used = used;
                TilesAllocated = tilesAllocated;
                TilesActual = tilesActual;
                StringsAllocated = stringsAllocated;
                StringsActual = stringsActual;
                UrisAllocated = urisAllocated;
                UrisActual = urisActual;
                Warm = warm;
                Hot = hot;
            }
        }

        public DataTelemetryDetailsViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }

        protected override VisualElement CreateView()
        {
            m_Root = new VisualElement();
            m_Root.style.flexDirection = FlexDirection.Column;
            m_Root.style.flexGrow = 1;

            m_Header = new Label("Select a frame to view per-DataSet telemetry.");
            m_Header.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Header.style.marginBottom = 6;
            m_Root.Add(m_Header);

            m_Table = CreateTable(m_Rows);
            m_Root.Add(m_Table);

            // React to frame selection changes in the Profiler window
            ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

            // Initial load
            ReloadFromSelectedFrame();

            return m_Root;
        }

        protected override void Dispose(bool disposing)
        {
            // Unsubscribe
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnSelectedFrameIndexChanged(long _)
        {
            ReloadFromSelectedFrame();
        }

        void ReloadFromSelectedFrame()
        {
            var frame = (int)ProfilerWindow.selectedFrameIndex;
            if (frame < 0)
            {
                SetHeader("No frame selected.");
                UpdateRows();
                return;
            }

            ReloadData(frame);
        }

        void ReloadData(int frameIndex)
        {
            // Thread index 0 is usually main thread; you can also iterate threads if you want.
            using var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0);
            if (frameData == null || !frameData.valid)
            {
                SetHeader($"Frame {frameIndex}: no profiler data available.");
                UpdateRows();
                return;
            }

            int chunkCount = frameData.GetFrameMetaDataCount(k_MetaId, k_MetaTag);
            if (chunkCount <= 0)
            {
                SetHeader($"Frame {frameIndex}: no telemetry metadata found.");
                UpdateRows();
                return;
            }

            m_Rows.Clear();

            // Merge chunks if you emit multiple times per frame for the same id/tag
            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                using NativeArray<TileSetMetaRow> rows = frameData.GetFrameMetaData<TileSetMetaRow>(k_MetaId, k_MetaTag, chunk);
                for (int i = 0; i < rows.Length; i++)
                {
                    var r = rows[i];
                    var name = DecodeFixedUtf8(r);

                    m_Rows.Add(new RowVm(
                        r.DataSetId,
                        name,
                        r.NativeReservedBytes,
                        r.NativeUsedBytes,
                        r.TilesAllocated,
                        r.TilesActual,
                        r.StringsAllocated,
                        r.StringsActual,
                        r.UrisAllocated,
                        r.UrisActual,
                        r.WarmCount,
                        r.HotCount
                    ));
                }
            }

            SetHeader($"Frame {frameIndex}: {m_Rows.Count} DataSets");
            UpdateRows();
        }

        void SetHeader(string text)
        {
            if (m_Header != null)
                m_Header.text = text;
        }

        void UpdateRows()
        {
            if (m_Table == null) return;

            m_Table.itemsSource = m_Rows;
            m_Table.Rebuild();
        }

        static unsafe string DecodeFixedUtf8(in TileSetMetaRow row)
        {
            fixed (byte* p = row.NameUtf8)
            {
                // Find first 0 byte
                int len = 0;
                for (; len < 64; len++)
                {
                    if (p[len] == 0) break;
                }

                if (len == 0)
                    return string.Empty;

                var span = new ReadOnlySpan<byte>(p, len);
                return Encoding.UTF8.GetString(span);
            }
        }

        static MultiColumnListView CreateTable(List<RowVm> backingRows)
        {
            var columns = new Columns();

            columns.Add(MakeCol("Id", 60, backingRows, vm => vm.Id.ToString()));
            columns.Add(MakeCol("Name", 220, backingRows, vm => vm.Name));
            columns.Add(MakeCol("Native Reserved", 130, backingRows, vm => FormatBytes(vm.Reserved)));
            columns.Add(MakeCol("Native Used", 120, backingRows, vm => FormatBytes(vm.Used)));
            columns.Add(MakeCol("Tiles Alloc", 90, backingRows, vm => vm.TilesAllocated.ToString()));
            columns.Add(MakeCol("Tiles Actual", 90, backingRows, vm => vm.TilesActual.ToString()));
            columns.Add(MakeCol("Strings Alloc", 90, backingRows, vm => vm.StringsAllocated.ToString()));
            columns.Add(MakeCol("Strings Actual", 90, backingRows, vm => vm.StringsActual.ToString()));
            columns.Add(MakeCol("Uris Alloc", 90, backingRows, vm => vm.UrisAllocated.ToString()));
            columns.Add(MakeCol("Uris Actual", 90, backingRows, vm => vm.UrisActual.ToString()));
            columns.Add(MakeCol("Warm", 70, backingRows, vm => vm.Warm.ToString()));
            columns.Add(MakeCol("Hot", 70, backingRows, vm => vm.Hot.ToString()));

            var list = new MultiColumnListView(columns)
            {
                fixedItemHeight = 18,
                showBorder = true,
                selectionType = SelectionType.Single,
                style =
                {
                    flexGrow = 1,
                    minHeight = 140
                }
            };

            return list;

            static Column MakeCol(string title, float width, List<RowVm> rows, Func<RowVm, string> getter)
            {
                var col = new Column
                {
                    name = title,
                    title = title,
                    width = width,
                    minWidth = 40,
                    stretchable = (title == "Name"),
                    resizable = true,
                    sortable = true
                };

                col.makeCell = () => new Label
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 4
                    }
                };

                col.bindCell = (element, rowIndex) =>
                {
                    if (element is not Label label) return;
                    if ((uint)rowIndex >= (uint)rows.Count) { label.text = ""; return; }

                    label.text = getter(rows[rowIndex]);
                };

                return col;
            }
        }

        static string FormatBytes(long bytes)
        {
            // Simple human-readable
            double b = bytes;
            if (b < 1024) return $"{bytes} B";
            b /= 1024;
            if (b < 1024) return $"{b:0.0} KB";
            b /= 1024;
            if (b < 1024) return $"{b:0.0} MB";
            b /= 1024;
            return $"{b:0.0} GB";
        }
    }
}
