#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Netherlands3D.Tilekit.WriteModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Tilekit.Editor
{
    public class TileHierarchyWindow : EditorWindow
    {
        private const string WindowTitle = "Tile Hierarchy (UITK)";

        private MonoBehaviour _currentService;
        private object _currentArchetype;
        private ColdStorage _coldStorage;

        private readonly Dictionary<int, TileTemperature> _tileStates = new();
        private VisualElement _headerContainer;
        private ScrollView _scrollView;

        private double _lastRefreshTime;
        private const double RefreshIntervalSeconds = 0.25;

        private enum TileTemperature
        {
            Cold,
            Warm,
            Hot
        }

        [MenuItem("Window/Tilekit/Tile Hierarchy (UITK)")]
        public static void ShowWindow()
        {
            var window = GetWindow<TileHierarchyWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(250, 200);
            window.Show();
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

            _headerContainer = new VisualElement
            {
                name = "header-container"
            };
            _headerContainer.style.paddingLeft = 6;
            _headerContainer.style.paddingRight = 6;
            _headerContainer.style.paddingTop = 6;
            _headerContainer.style.paddingBottom = 6;
            _headerContainer.style.borderBottomWidth = 1;
            _headerContainer.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);
            _headerContainer.style.borderBottomLeftRadius = 2;
            _headerContainer.style.borderBottomRightRadius = 2;

            rootVisualElement.Add(_headerContainer);

            _scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "tiles-scrollview"
            };
            _scrollView.style.flexGrow = 1.0f;
            rootVisualElement.Add(_scrollView);

            UpdateSelectionFromActive();
            RefreshUI();
        }

        #region Editor update / selection

        private void OnSelectionChanged()
        {
            UpdateSelectionFromActive();
            _currentArchetype = null;
            _coldStorage = null;
            RefreshUI();
        }

        private void OnEditorUpdate()
        {
            // if (!Application.isPlaying) return;
            //
            // var now = EditorApplication.timeSinceStartup;
            // if (now - _lastRefreshTime > RefreshIntervalSeconds)
            // {
            //     _lastRefreshTime = now;
            //     RefreshUI();
            // }
        }

        private void UpdateSelectionFromActive()
        {
            _currentService = FindServiceOnSelection();
        }

        private MonoBehaviour FindServiceOnSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null) return null;

            var components = go.GetComponents<MonoBehaviour>();
            foreach (var mb in components)
            {
                if (mb == null) continue;
                var type = mb.GetType();
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DataSet<,,>))
                    {
                        return mb;
                    }

                    type = type.BaseType;
                }
            }

            return null;
        }

        #endregion

        #region UI refresh

        private void RefreshUI()
        {
            if (_headerContainer == null || _scrollView == null)
                return;

            _headerContainer.Clear();
            _scrollView.Clear();

            DrawHeaderBasics();

            if (!Application.isPlaying)
            {
                _scrollView.Add(new HelpBox("Tile Hierarchy is only available in Play Mode.", HelpBoxMessageType.Info));
                return;
            }

            if (_currentService == null)
            {
                _scrollView.Add(new HelpBox(
                    "Select a GameObject with a ServiceType<,,> component to inspect its tiles.",
                    HelpBoxMessageType.Info));
                return;
            }

            if (!TryAcquireArchetypeAndColdStorage(out var notReadyReason))
            {
                _scrollView.Add(new HelpBox(notReadyReason, HelpBoxMessageType.Info));
                return;
            }

            // Build tile state cache (cold/warm/hot).
            BuildTileStateCache();

            DrawHeaderDetails();

            if (_coldStorage.GeometricError.Length == 0)
            {
                _scrollView.Add(new HelpBox("ColdStorage is empty (no tiles).", HelpBoxMessageType.Info));
                return;
            }

            // Root hierarchy
            var rootTile = _coldStorage.Root;
            var rootElement = CreateTileFoldout(rootTile);
            _scrollView.Add(rootElement);
        }

        private void DrawHeaderBasics()
        {
            var titleLabel = new Label(WindowTitle)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    marginBottom = 2
                }
            };

            _headerContainer.Add(titleLabel);

            if (_currentService != null)
            {
                _headerContainer.Add(new Label($"GameObject: {_currentService.gameObject.name}"));
                _headerContainer.Add(new Label($"Component: {_currentService.GetType().Name}"));
            }
            else
            {
                _headerContainer.Add(new Label("No ServiceType selected."));
            }
        }

        private void DrawHeaderDetails()
        {
            int totalTiles = _coldStorage.GeometricError.Length;
            int warmCount = 0;
            int hotCount = 0;

            foreach (var kvp in _tileStates)
            {
                if (kvp.Value == TileTemperature.Warm) warmCount++;
                else if (kvp.Value == TileTemperature.Hot) hotCount++;
            }

            var container = new VisualElement();
            container.style.marginTop = 4;
            container.style.marginBottom = 2;
            container.style.paddingTop = 2;
            container.style.borderTopWidth = 1;
            container.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f);

            container.Add(new Label("Tile counts")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 2
                }
            });
            container.Add(new Label($"Total (ColdStorage): {totalTiles}"));
            container.Add(new Label($"Warm: {warmCount}"));
            container.Add(new Label($"Hot:  {hotCount}"));

            _headerContainer.Add(container);
        }

        #endregion

        #region Reflection / archetype

        private bool TryAcquireArchetypeAndColdStorage(out string reasonNotReady)
        {
            reasonNotReady = null;

            if (_currentService == null)
            {
                reasonNotReady = "No ServiceType<,,> found on the selected GameObject.";
                return false;
            }

            var serviceType = _currentService.GetType();

            var isInitializedProp = serviceType.GetProperty("IsInitialized",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (isInitializedProp != null)
            {
                var isInitialized = (bool)isInitializedProp.GetValue(_currentService);
                if (!isInitialized)
                {
                    reasonNotReady = "ServiceType is not initialized yet. Wait until it has created its Archetype.";
                    return false;
                }
            }

            if (_currentArchetype == null)
            {
                var archetypeField = FindFieldInHierarchy(serviceType, "archetype");
                if (archetypeField == null)
                {
                    reasonNotReady = "Could not find 'archetype' field on ServiceType.";
                    return false;
                }

                _currentArchetype = archetypeField.GetValue(_currentService);
                if (_currentArchetype == null)
                {
                    reasonNotReady = "Archetype has not been created yet.";
                    return false;
                }
            }

            if (_coldStorage == null)
            {
                var archetypeType = _currentArchetype.GetType();
                var coldProp = archetypeType.GetProperty("Cold",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (coldProp == null)
                {
                    reasonNotReady = "Archetype does not expose a 'Cold' property.";
                    return false;
                }

                _coldStorage = coldProp.GetValue(_currentArchetype) as ColdStorage;
                if (_coldStorage == null)
                {
                    reasonNotReady = "ColdStorage instance is null.";
                    return false;
                }
            }

            return true;
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                    return field;
                type = type.BaseType;
            }

            return null;
        }

        #endregion

        #region Tile state

        private void BuildTileStateCache()
        {
            _tileStates.Clear();

            var archetypeType = _currentArchetype.GetType();
            var warmField = archetypeType.GetField("warm",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var hotField = archetypeType.GetField("hot",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (warmField == null || hotField == null)
                return;

            var warmListObj = warmField.GetValue(_currentArchetype);
            var hotListObj = hotField.GetValue(_currentArchetype);

            if (warmListObj == null || hotListObj == null)
                return;

            var warmType = warmListObj.GetType();
            var hotType = hotListObj.GetType();

            var warmLengthProp = warmType.GetProperty("Length");
            var hotLengthProp = hotType.GetProperty("Length");

            var warmItemProp = warmType.GetProperty("Item");
            var hotItemProp = hotType.GetProperty("Item");

            if (warmLengthProp == null || hotLengthProp == null || warmItemProp == null || hotItemProp == null)
                return;

            int warmLength = (int)warmLengthProp.GetValue(warmListObj);
            int hotLength = (int)hotLengthProp.GetValue(hotListObj);

            var warmIndexToTileIndex = new int[warmLength];

            // Warm
            for (int i = 0; i < warmLength; i++)
            {
                var warmItem = warmItemProp.GetValue(warmListObj, new object[] { i });
                if (warmItem == null) continue;

                var warmItemType = warmItem.GetType();
                var tileIndexProp = warmItemType.GetProperty("TileIndex");
                if (tileIndexProp == null) continue;

                int tileIndex = (int)tileIndexProp.GetValue(warmItem);
                warmIndexToTileIndex[i] = tileIndex;
                _tileStates[tileIndex] = TileTemperature.Warm;
            }

            // Hot
            for (int i = 0; i < hotLength; i++)
            {
                var hotItem = hotItemProp.GetValue(hotListObj, new object[] { i });
                if (hotItem == null) continue;

                var hotItemType = hotItem.GetType();
                var warmIndexProp = hotItemType.GetProperty("WarmTileIndex");
                if (warmIndexProp == null) continue;

                int warmIndex = (int)warmIndexProp.GetValue(hotItem);
                if (warmIndex < 0 || warmIndex >= warmIndexToTileIndex.Length) continue;

                int tileIndex = warmIndexToTileIndex[warmIndex];
                _tileStates[tileIndex] = TileTemperature.Hot;
            }

            // Everything else is implicitly Cold.
        }

        private TileTemperature GetTileTemperature(int tileIndex)
        {
            if (_tileStates.TryGetValue(tileIndex, out var temp))
                return temp;

            return TileTemperature.Cold;
        }

        private Color GetTemperatureColor(TileTemperature temperature)
        {
            switch (temperature)
            {
                case TileTemperature.Hot:
                    return new Color(1.0f, 0.3f, 0.2f); // red-ish
                case TileTemperature.Warm:
                    return new Color(1.0f, 0.8f, 0.2f); // yellow-ish
                default:
                    return new Color(0.5f, 0.5f, 0.5f); // grey
            }
        }

        #endregion

        #region UITK tree building

        private VisualElement CreateTileFoldout(Tile tile)
        {
            var tileIndex = tile.Index;
            var temperature = GetTileTemperature(tileIndex);
            var color = GetTemperatureColor(temperature);

            var topLeft = tile.BoundingVolume.ToBounds().Min;
            var bottomRight = tile.BoundingVolume.ToBounds().Max;
            var foldout = new Foldout
            {
                text = new StringBuilder()
                    .Append("Tile ")
                    .Append(tileIndex)
                    .Append(" (")
                    .Append(topLeft.x.ToString("0.##")).Append(",").Append(topLeft.y.ToString("0.##"))
                    .Append(" / ")
                    .Append(bottomRight.x.ToString("0.##")).Append(",").Append(bottomRight.y.ToString("0.##"))
                    .Append(")")
                    .ToString(),
                value = true
            };

            // Add colored circle into the foldout's toggle
            var toggle = foldout.Q<Toggle>();
            if (toggle != null)
            {
                var icon = new VisualElement
                {
                    style =
                    {
                        width = 10,
                        height = 10,
                        marginRight = 4,
                        marginTop = 2,
                        marginBottom = 2,
                        backgroundColor = color,
                        borderBottomRightRadius = 5,
                        borderBottomLeftRadius = 5,
                        borderTopRightRadius = 5,
                        borderTopLeftRadius = 5
                    }
                };

                // Put icon before the label
                toggle.contentContainer.Insert(0, icon);
            }

            // Clicking a row could ping the tile's layer object later if you want
            // (for now it's just a visual tree).

            var children = tile.Children();
            for (int i = 0; i < children.Count; i++)
            {
                var child = tile.GetChild(i);
                if (child.Index != -1)
                    foldout.Add(CreateTileFoldout(child));
            }

            return foldout;
        }

        #endregion
    }
}
#endif
