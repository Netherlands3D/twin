using System;
using System.Collections;
using KindMen.Uxios;
using Netherlands3D.Tilekit.BoundingVolumes;
using SimpleJSON;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ServiceTypes
{
    [RequireComponent(typeof(Timer))]
    public class Ogc3DTiles : MonoBehaviour
    {
        public string Url;
        public Timer timer;
        
        private IEnumerator Start()
        {
            timer = GetComponent<Timer>();

            // Wait two frames for the switch to main scene
            yield return null;
            yield return null;

            var promise = Uxios.DefaultInstance.Get<string>(new Uri(Url));
            promise.Then(OnLoad).Catch(Debug.LogException);
        }

        private void OnEnable()
        {
            timer.tick.AddListener(OnTick);
        }

        private void OnDisable()
        {
            timer.tick.RemoveListener(OnTick);
        }

        private void OnLoad(IResponse bytes)
        {
            // The use of SimpleJSON still creates allocations - for the prototype we are making it is good enough. Making the JSON parsing 
            // zalloc is our next problem
            var json = (bytes.Data as string);
            JSONNode rootnode = JSON.Parse(json)["root"];

            // TODO: Are we building the working set or a full set? if this is the working set, we need to rebuild on every major relocation of the camera
            
            var tileSet = new TileSet();
            var boxBoundingVolume = rootnode["boundingVolume"]["box"];

            var transformNode = rootnode.HasKey("transform") ? rootnode["transform"] : null;
            tileSet.AddTile(
                BoxBoundingVolumeFromJsonNodeArray(boxBoundingVolume),
                rootnode["geometricError"].AsFloat,
                rootnode.HasKey("refine") && rootnode["refine"] == "REPLACE" ? MethodOfRefinement.Replace : MethodOfRefinement.Add,
                rootnode.HasKey("implicitTiling") ? SubdivisionScheme.Quadtree : SubdivisionScheme.None, // TODO: Inaccurate
                CreateTransformMatrixFromJsonNode(transformNode),
                new ReadOnlySpan<int>(), 
                new ReadOnlySpan<TileContentData>() 
            );
            
            // TODO: Create a working set and store that somehow - or is that this already?
            
            timer.Resume();
        }

        public void OnTick()
        {
        }

        private BoxBoundingVolume BoxBoundingVolumeFromJsonNodeArray(JSONNode node)
        {
            return new BoxBoundingVolume(
                new double3(
                    node[0].AsDouble,
                    node[1].AsDouble,
                    node[2].AsDouble
                ),
                new double3(
                    node[3].AsDouble,
                    node[4].AsDouble,
                    node[5].AsDouble
                ),
                new double3(
                    node[6].AsDouble,
                    node[7].AsDouble,
                    node[8].AsDouble
                ),
                new double3(
                    node[9].AsDouble,
                    node[10].AsDouble,
                    node[11].AsDouble
                )
            );
        }

        private static float4x4 CreateTransformMatrixFromJsonNode(JSONNode transformNode)
        {
            if (transformNode == null) return float4x4.identity;

            return new float4x4(
                transformNode[0].AsFloat, 
                transformNode[1].AsFloat, 
                transformNode[2].AsFloat, 
                transformNode[3].AsFloat, 
                transformNode[4].AsFloat, 
                transformNode[5].AsFloat, 
                transformNode[6].AsFloat, 
                transformNode[7].AsFloat, 
                transformNode[8].AsFloat, 
                transformNode[9].AsFloat, 
                transformNode[10].AsFloat, 
                transformNode[11].AsFloat, 
                transformNode[12].AsFloat, 
                transformNode[13].AsFloat, 
                transformNode[14].AsFloat, 
                transformNode[15].AsFloat 
            );
        }
    }
}