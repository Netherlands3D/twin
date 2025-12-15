using System;
using System.Collections.Generic;
using System.Text;
using Netherlands3D.Tilekit.Geometry;
using Netherlands3D.Tilekit.WriteModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Tilekit.Editor
{
    public class TileHierarchyWindow : EditorWindow
    {
        private const string WindowTitle = "Tile Hierarchy (UITK)";

        private DataSet _dataSet;
        private TileSet _tileSet;

        private readonly Dictionary<int, TileTemperature> _tileStates = new();

        // New: parent links + active path cache (parents of warm/hot tiles)
        private readonly Dictionary<int, int> _parentByTile = new();
        private readonly HashSet<int> _activePath = new();

        private VisualElement _header;
        private TreeView _treeView;

        private double _lastRefresh;
        private const double RefreshIntervalSeconds = 0.5;

        private enum TileTemperature
        {
            Cold,
            Warm,
            Hot
        }

        [MenuItem("Window/Tilekit/Tile Hierarchy (UITK)")]
        public static void ShowWindow()
        {
            var window = GetWindow<TileHierarchyWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(320, 200);
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            _header = new VisualElement();
            _header.style.paddingLeft = 6;
            _header.style.paddingRight = 6;
            _header.style.paddingTop = 6;
            _header.style.paddingBottom = 6;
            _header.style.borderBottomWidth = 1;
            _header.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);
            rootVisualElement.Add(_header);

            _treeView = CreateTreeView();
            rootVisualElement.Add(_treeView);

            UpdateSelection();
            RebuildTreeIfPossible();
        }

        #region Selection / update

        private void OnSelectionChanged()
        {
            UpdateSelection();
            RebuildTreeIfPossible();
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _tileSet == null)
                return;

            var now = EditorApplication.timeSinceStartup;
            if (now - _lastRefresh > RefreshIntervalSeconds)
            {
                _lastRefresh = now;

                BuildTileStateCache();
                BuildActivePathCache();

                _treeView.RefreshItems(); // cheap: rebind visible rows only
            }
        }

        private void UpdateSelection()
        {
            var go = Selection.activeGameObject;
            _dataSet = go ? go.GetComponentInParent<DataSet>() : null;

            _tileSet = (_dataSet != null && _dataSet.IsInitialized)
                ? _dataSet.TileSet
                : null;
        }

        #endregion

        #region UI / Tree creation

        private TreeView CreateTreeView()
        {
            var tv = new TreeView
            {
                style = { flexGrow = 1 },
                selectionType = SelectionType.Single,
                fixedItemHeight = 18,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };

            tv.makeItem = MakeItem;
            tv.bindItem = BindItem;

            return tv;
        }

        private VisualElement MakeItem()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 18
                }
            };

            // New: white “path” indicator
            var pathBar = new VisualElement
            {
                name = "path-bar",
                style =
                {
                    width = 3,
                    height = 12,
                    marginRight = 6,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    backgroundColor = Color.clear
                }
            };

            var dot = new VisualElement
            {
                name = "temp-dot",
                style =
                {
                    width = 8,
                    height = 8,
                    marginRight = 6,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4
                }
            };

            var label = new Label { name = "label" };
            label.style.unityTextAlign = TextAnchor.MiddleLeft;

            row.Add(pathBar);
            row.Add(dot);
            row.Add(label);
            return row;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (_tileSet == null)
                return;

            // TreeView index -> tileIndex
            int tileIndex = _treeView.GetItemDataForIndex<int>(index);

            BoundsDouble bounds = _tileSet.GetBoundingVolume(tileIndex).ToBounds();
            var min = bounds.Min;
            var max = bounds.Max;

            var label = element.Q<Label>("label");
            label.text = new StringBuilder(64)
                .Append("Tile ").Append(tileIndex)
                .Append(" (")
                .Append(min.x.ToString("0.##")).Append(",").Append(min.y.ToString("0.##"))
                .Append(" / ")
                .Append(max.x.ToString("0.##")).Append(",").Append(max.y.ToString("0.##"))
                .Append(")")
                .ToString();

            var temp = GetTileTemperature(tileIndex);

            var dot = element.Q<VisualElement>("temp-dot");
            dot.style.backgroundColor = GetTemperatureColor(temp);

            var pathBar = element.Q<VisualElement>("path-bar");

            // Show white indicator for ancestors/path nodes (not on the warm/hot tile itself by default)
            bool isWarmOrHot = temp != TileTemperature.Cold;
            bool isOnActivePath = _activePath.Contains(tileIndex);

            pathBar.style.backgroundColor = (!isWarmOrHot && isOnActivePath)
                ? Color.white
                : Color.clear;
        }

        private void RebuildTreeIfPossible()
        {
            _header.Clear();

            if (_dataSet == null)
            {
                _header.Add(new Label("Select a GameObject with a DataSet component."));
                _treeView.SetRootItems(Array.Empty<TreeViewItemData<int>>());
                _treeView.Rebuild();
                return;
            }

            _header.Add(new Label($"GameObject: {_dataSet.gameObject.name}"));
            _header.Add(new Label($"Component: {_dataSet.GetType().Name}"));
            _header.Add(new Label($"Initialized: {_dataSet.IsInitialized}"));

            if (!Application.isPlaying || !_dataSet.IsInitialized || _tileSet == null || _tileSet.GeometricError.Length == 0)
            {
                _treeView.SetRootItems(Array.Empty<TreeViewItemData<int>>());
                _treeView.Rebuild();
                return;
            }

            // Rebuild parent table while building the tree
            _parentByTile.Clear();

            BuildTileStateCache();

            var visited = new HashSet<int>(Math.Min(_tileSet.GeometricError.Length, 8192));
            var root = BuildItemRecursive(tileIndex: 0, parentIndex: -1, visited);

            _treeView.SetRootItems(new List<TreeViewItemData<int>>(1) { root });
            _treeView.Rebuild();

            // Must be after parent map is ready
            BuildActivePathCache();
            _treeView.RefreshItems();
        }

        /// <summary>
        /// Builds TreeViewItemData recursively from TileSet children buffers.
        /// Leaf tiles return children=null => no expander indicator.
        /// Also fills _parentByTile for active-path highlighting.
        /// </summary>
        private TreeViewItemData<int> BuildItemRecursive(int tileIndex, int parentIndex, HashSet<int> visited)
        {
            if (!visited.Add(tileIndex))
                return new TreeViewItemData<int>(tileIndex, tileIndex, children: null);

            if (parentIndex >= 0)
                _parentByTile[tileIndex] = parentIndex;

            var childrenBlock = _tileSet.GetChildren(tileIndex);

            if (childrenBlock.Length == 0)
                return new TreeViewItemData<int>(tileIndex, tileIndex, children: null);

            var children = new List<TreeViewItemData<int>>(childrenBlock.Length);
            for (int i = 0; i < childrenBlock.Length; i++)
            {
                int childIndex = childrenBlock[i];
                if (childIndex < 0) continue;

                children.Add(BuildItemRecursive(childIndex, tileIndex, visited));
            }

            // If all children were invalid, still treat as leaf (no expander).
            if (children.Count == 0)
                return new TreeViewItemData<int>(tileIndex, tileIndex, children: null);

            return new TreeViewItemData<int>(tileIndex, tileIndex, children);
        }

        #endregion

        #region Tile temperature + active path

        private void BuildTileStateCache()
        {
            _tileStates.Clear();

            for (int i = 0; i < _tileSet.Warm.Length; i++)
                _tileStates[_tileSet.Warm[i]] = TileTemperature.Warm;

            for (int i = 0; i < _tileSet.Hot.Length; i++)
                _tileStates[_tileSet.Hot[i]] = TileTemperature.Hot;
        }

        private void BuildActivePathCache()
        {
            _activePath.Clear();

            // Mark ancestors of all warm tiles
            for (int i = 0; i < _tileSet.Warm.Length; i++)
                AddAncestors(_tileSet.Warm[i]);

            // Mark ancestors of all hot tiles
            for (int i = 0; i < _tileSet.Hot.Length; i++)
                AddAncestors(_tileSet.Hot[i]);
        }

        private void AddAncestors(int tileIndex)
        {
            int current = tileIndex;

            // Add current and walk upward using parent map
            while (_activePath.Add(current) && _parentByTile.TryGetValue(current, out var parent))
            {
                current = parent;
            }
        }

        private TileTemperature GetTileTemperature(int tileIndex)
        {
            return _tileStates.TryGetValue(tileIndex, out var t) ? t : TileTemperature.Cold;
        }

        private static Color GetTemperatureColor(TileTemperature t)
        {
            return t switch
            {
                TileTemperature.Hot => new Color(1.0f, 0.3f, 0.2f),
                TileTemperature.Warm => new Color(1.0f, 0.8f, 0.2f),
                _ => new Color(0.55f, 0.55f, 0.55f)
            };
        }

        #endregion
    }
}
