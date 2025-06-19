using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DXFCreation : ModelFormatCreation
{
    [DllImport("__Internal")]
    private static extern void DownloadFileImmediate(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);

    private BoundingBox boundingBox;

    protected override IEnumerator CreateFile(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight, bool destroyOnCompletion = true)
    {
        // FreezeLayers(layerList, true);
        Coordinate bottomLeftRD = new Coordinate(selectedAreaBounds.min);
        Coordinate topRightRD = new Coordinate(selectedAreaBounds.max);
        boundingBox = new BoundingBox(bottomLeftRD, topRightRD);
        DxfFile dxfFile = new DxfFile();
        dxfFile.SetupDXF();
        yield return null;
        // MeshClipper meshClipper = new MeshClipper();

        // loadingScreen.ShowMessage("DXF-bestand genereren...");
        // loadingScreen.ProgressBar.SetMessage("");
        // loadingScreen.ProgressBar.Percentage(0.1f);
        // yield return new WaitForEndOfFrame();

        // int layercounter = 0;
        // foreach (var layer in layerList)
        // {
        //     layercounter++;
        // loadingScreen.ProgressBar.Percentage((float)layercounter / ((float)layerList.Count+1));
        // loadingScreen.ProgressBar.SetMessage("Laag '" + layer.name + "' wordt omgezet...");
        // yield return new WaitForEndOfFrame();

        // List<GameObject> gameObjectsToClip = GetTilesInLayer(layer, bottomLeftRD, topRightRD);
        // if (gameObjectsToClip.Count==0)
        // {
        //     continue;
        // }
        // foreach (var gameObject in gameObjectsToClip)
        // {
        //     meshClipper.SetGameObject(gameObject);
        //     for (int submeshID = 0; submeshID < gameObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount; submeshID++)
        //     {
        //         string layerName = gameObject.GetComponent<MeshRenderer>().sharedMaterials[submeshID].name.Replace(" (Instance)","");
        //         layerName = layerName.Replace("=", "");
        //         layerName = layerName.Replace("\\", "");
        //         layerName = layerName.Replace("<", "");
        //         layerName = layerName.Replace(">", "");
        //         layerName = layerName.Replace("/", "");
        //         layerName = layerName.Replace("?", "");
        //         layerName = layerName.Replace("\"" ,"");
        //         layerName = layerName.Replace(":", "");
        //         layerName = layerName.Replace(";", "");
        //         layerName = layerName.Replace("*", "");
        //         layerName = layerName.Replace("|", "");
        //         layerName = layerName.Replace(",", "");
        //         layerName = layerName.Replace("'", "");
        //
        //         // loadingScreen.ProgressBar.SetMessage("Laag '" + layer.name + "' object " + layerName + " wordt uitgesneden...");
        //         yield return new WaitForEndOfFrame();
        //         
        //         meshClipper.ClipSubMesh(boundingBox.ToUnityBounds(), submeshID);
        var objects = GetExportData(includedLayers, selectedAreaBounds, minClipBoundsHeight);

        // var verts = meshClipper.clippedVertices;
        var vertsRD = new List<Vector3RD>();
        foreach (var obj in objects)
        {
            foreach (var v in obj.Vertices)
            {
                var coord = new Coordinate(v);
                coord = coord.Convert(CoordinateSystem.RD);
                vertsRD.Add(coord.ToVector3RD());
            }

            dxfFile.AddLayer(vertsRD, obj.Name, GetColor(obj.Material));
        }

        yield return null;

#if UNITY_EDITOR
        var localFile = EditorUtility.SaveFilePanel("Save Dxf", "", "export", "dxf");
        if (localFile.Length > 0)
        {
            dxfFile.Save(localFile);
        }
        //todo:
#elif UNITY_WEBGL
                // byte[] byteArray = Encoding.UTF8.GetBytes(colladaFile.GetColladaXML());
                // DownloadFileImmediate(gameObject.name, "", "Collada.dae", byteArray, byteArray.Length);
#endif
        Debug.Log("file saved");

        if (destroyOnCompletion)
            Destroy(gameObject);
    }

    private netDxf.AciColor GetColor(Material material)
    {
        if (material.GetColor("_BaseColor") != null)
        {
            byte r = (byte)(material.GetColor("_BaseColor").r * 255);
            byte g = (byte)(material.GetColor("_BaseColor").g * 255);
            byte b = (byte)(material.GetColor("_BaseColor").b * 255);
            return new netDxf.AciColor(r, g, b);
        }
        
        if (material.GetColor("_FresnelColorHigh") != null)
        {
            byte r = (byte)(material.GetColor("_FresnelColorHigh").r * 255);
            byte g = (byte)(material.GetColor("_FresnelColorHigh").g * 255);
            byte b = (byte)(material.GetColor("_FresnelColorHigh").b * 255);
            return new netDxf.AciColor(r, g, b);
        }
        else
        {
            return netDxf.AciColor.LightGray;
        }
    }
}