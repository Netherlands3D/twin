using System;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.SubObjects;
using Netherlands3D.Coordinates;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area

        public Layer Layer => layer;

        private Layer layer;
        private TileHandler tileHandler;

        public override IStyler Styler 
        {  
            get 
            {
                if (styler == null)
                {
                    styler = new CartesianTileLayerStyler(this);
                }
                return styler;
            } 
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!layer || layer.isEnabled == isActive) return;

            layer.isEnabled = isActive;
        }

        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);

            SetupFeatures();
        }

        /// <summary>
        /// Cartesian Tiles have 'virtual' features, each type of terrain (grass, cycling path, etc) can be styled
        /// independently and thus is a feature. At the moment, the most concrete list of criteria for which features
        /// exist is the list of materials per terrain type.
        ///
        /// As such we create a LayerFeature per material with the material name and index as attribute, this allows
        /// for the styling system to apply styles per material - and thus: per feature type. 
        /// </summary>
        private void SetupFeatures()
        {
            if (layer is not BinaryMeshLayer binaryMeshLayer) return;

            //we have to apply styling when mappings are created, before we cannot load the values like from awake
            binaryMeshLayer.OnMappingCreated.AddListener(OnAddedMapping);         
            binaryMeshLayer.OnMappingRemoved.AddListener(OnRemovedMapping);

            for (var materialIndex = 0; materialIndex < binaryMeshLayer.DefaultMaterialList.Count; materialIndex++)
            {
                // Make a copy of the default material, so we can change the color without affecting the original
                // TODO: This should be part of the BinaryMeshLayer itself?
                var material = binaryMeshLayer.DefaultMaterialList[materialIndex];
                material = new Material(material);
                binaryMeshLayer.DefaultMaterialList[materialIndex] = material;

                var layerFeature = CreateFeature(material);
                LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
        }

        private void OnAddedMapping(ObjectMapping mapping)
        {         
            foreach (ObjectMappingItem item in mapping.items)
            {
                var layerFeature = CreateFeature(item);
                LayerFeatures.Add(layerFeature.Geometry, layerFeature);
            }
            ApplyStyling();
        }

        private void OnRemovedMapping(ObjectMapping mapping)
        {
            foreach (ObjectMappingItem item in mapping.items)
            {
                LayerFeatures.Remove(item);
            }
        }

        public ObjectMapping FindObjectMapping(ObjectMappingItem item)
        {
            if (layer is not BinaryMeshLayer binaryMeshLayer) return null;

            return FindObjectMapping(item.objectID);
        }

        //TODO, this should be optimized
        public ObjectMapping FindObjectMapping(string bagId)
        {
            if (layer is not BinaryMeshLayer binaryMeshLayer) return null;

            foreach (ObjectMapping mapping in binaryMeshLayer.Mappings.Values)
            {
                foreach(ObjectMappingItem item in mapping.items)
                {
                    if (item.objectID == bagId)
                        return mapping;
                }
            }
            return null;
        }

        public LayerFeature GetLayerFeatureFromBagId(string bagId)
        {
            if (layer is not BinaryMeshLayer binaryMeshLayer) return null;

            foreach (ObjectMapping mapping in binaryMeshLayer.Mappings.Values)
            {
                foreach (ObjectMappingItem item in mapping.items)
                {
                    if (item.objectID == bagId)
                    {
                        var layerFeature = CreateFeature(item);
                        return layerFeature;
                    }
                }
            }
            return null;
        }

        public Coordinate GetCoordinateForObjectMappingItem(ObjectMapping objectMapping, ObjectMappingItem mapping)
        {
            MeshFilter mFilter = objectMapping.gameObject.GetComponent<MeshFilter>();
            Vector3[] vertices = mFilter.sharedMesh.vertices;
            Vector3 centr = Vector3.zero;
            for (int i = mapping.firstVertex; i < mapping.firstVertex + mapping.verticesLength; i++)
                centr += vertices[i];
            centr /= mapping.verticesLength;

            //DebugVertices(vertices, mapping.firstVertex, mapping.verticesLength, mFilter.transform);

            Vector3 centroidWorld = mFilter.transform.TransformPoint(centr);
            Coordinate coord = new Coordinate(centroidWorld);
            return coord;
        }

        public static Mesh CreateMeshFromMapping(ObjectMapping objectMapping, ObjectMappingItem mapping, out Vector3 localCentroid, bool centerMesh = true)
        {
            var srcTf = objectMapping.gameObject.transform;
            MeshFilter mf = objectMapping.gameObject.GetComponent<MeshFilter>();
            Mesh src = mf.sharedMesh;

            Vector3[] srcV = src.vertices;
            Vector3[] srcN = src.normals;
            int[] srcT = src.triangles;

            int start = mapping.firstVertex;
            int len = mapping.verticesLength;

            // compute centroid in source mesh local space
            localCentroid = Vector3.zero;
            for (int i = 0; i < len; i++)
                localCentroid += srcV[start + i];
            localCentroid /= Mathf.Max(1, len);

            // copy and optionally center vertices
            Vector3[] newV = new Vector3[len];
            Vector3[] newN = (srcN != null && srcN.Length == srcV.Length) ? new Vector3[len] : null;
            for (int i = 0; i < len; i++)
            {
                var v = srcV[start + i];
                newV[i] = centerMesh ? (v - localCentroid) : v;
                if (newN != null) newN[i] = srcN[start + i];
            }

            // remap triangles that are fully inside the selected vertex range
            var newTris = new List<int>();
            for (int i = 0; i < srcT.Length; i += 3)
            {
                int a = srcT[i], b = srcT[i + 1], c = srcT[i + 2];
                if (a >= start && a < start + len &&
                    b >= start && b < start + len &&
                    c >= start && c < start + len)
                {
                    newTris.Add(a - start);
                    newTris.Add(b - start);
                    newTris.Add(c - start);
                }
            }

            var mesh = new Mesh();
            mesh.vertices = newV;
            if (newN != null) mesh.normals = newN;
            mesh.triangles = newTris.ToArray();
            mesh.RecalculateBounds();
            if (newN == null || newN.Length == 0) mesh.RecalculateNormals();

            return mesh;
        }

        private void DebugVertices(Vector3[] vertices, int start, int length, Transform transform)
        {
            for (int i = start; i < start + length; i++)
            {
                Vector3 vertexWorld = transform.TransformPoint(vertices[i]);

                GameObject testPos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vertexWorld.y = 50;
                testPos.transform.position = vertexWorld;
                testPos.GetComponent<MeshRenderer>().material.color = Color.green;
                testPos.transform.localScale = Vector3.one * 5;
            }
        }


        public override void OnSelect()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(true);
        }

        public override void OnDeselect()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(layer is BinaryMeshLayer binaryMeshLayer)
            {
                binaryMeshLayer.OnMappingCreated.RemoveListener(OnAddedMapping);
                binaryMeshLayer.OnMappingRemoved.RemoveListener(OnRemovedMapping);

            }
            if (Application.isPlaying && tileHandler && layer)
            {
                tileHandler.RemoveLayer(layer);
            }

        }

        private List<IPropertySectionInstantiator> propertySections;

        protected List<IPropertySectionInstantiator> PropertySections
        {
            get
            {
                if (propertySections == null)
                {
                    propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
                }

                return propertySections;
            }
            set => propertySections = value;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return PropertySections;
        }

        public override void ApplyStyling()
        {
            // WMS and other projection layers also use this class as base - but they should not apply this styling
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                foreach (var (_, feature) in LayerFeatures)
                {
                    (Styler as CartesianTileLayerStyler).Apply(GetStyling(feature), feature);
                }
            }


            base.ApplyStyling();
        }

        public override void UpdateMaskBitMask(int bitmask)
        {
            if (layer is BinaryMeshLayer binaryMeshLayer)
            {
                UpdateBitMaskForMaterials(bitmask, binaryMeshLayer.DefaultMaterialList);
            }
        }

        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            // WMS and other projection layers also use this class as base - but they should not add their materials
            if (layer is not BinaryMeshLayer meshLayer) return feature;

            if(feature.Geometry is ObjectMappingItem item)
            {
                feature.Attributes.Add(CartesianTileLayerStyler.VisibilityIdentifier, item.objectID); 
            }

            if (feature.Geometry is not Material mat) return feature;

            feature.Attributes.Add(
                CartesianTileLayerStyler.MaterialIndexIdentifier,
                meshLayer.DefaultMaterialList.IndexOf(mat).ToString()
            );
            feature.Attributes.Add(CartesianTileLayerStyler.MaterialNameIdentifier, mat.name);

            return feature;
        }
    }
}