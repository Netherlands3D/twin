using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Netherlands3D.Coordinates;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_WEBGL
// ReSharper disable once RedundantUsingDirective
using System.IO;
#endif

namespace Netherlands3D.Dxf
{
    public class DXFCreation : ModelFormatCreation
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string callbackGameObjectName, string callbackMethodName, string fileName, byte[] array, int byteLength);
        public override bool KeepBoundsOffsetFromOrigin { get; set; } = true;

        protected override IEnumerator CreateFile(LayerMask includedLayers, Bounds selectedAreaBounds, float minClipBoundsHeight, bool destroyOnCompletion = true)
        {
            // FreezeLayers(layerList, true);
            DxfFile dxfFile = new DxfFile();
            dxfFile.SetupDXF();
            yield return null;

            var objects = GetExportData(includedLayers, selectedAreaBounds, minClipBoundsHeight);

            var vertsRD = new List<Vector3RD>();
            foreach (var obj in objects)
            {
                foreach (var v in obj.Vertices)
                {
                    var coord = new Coordinate(v);
                    coord = coord.Convert(CoordinateSystem.RDNAP);
                    vertsRD.Add(coord.ToVector3RD());
                }

                dxfFile.AddLayer(vertsRD, obj.Name, GetColor(obj.Material));
                vertsRD.Clear();
            }

            yield return null;

#if UNITY_EDITOR
            var localFile = EditorUtility.SaveFilePanel("Save Dxf", "", "export", "dxf");
            if (localFile.Length > 0)
            {
                dxfFile.Save(localFile);
            }
#elif UNITY_WEBGL
        using (var stream = new MemoryStream())
        {
            if (dxfFile.Save(stream))
            {
                DownloadFileImmediate(gameObject.name, "","export.dxf", stream.ToArray(), stream.ToArray().Length);
                Debug.Log("file saved");
            }
            else
            {
                Debug.Log("cant write file");
            }
        }
#endif

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
}