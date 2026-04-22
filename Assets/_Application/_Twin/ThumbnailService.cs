using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D
{
    public class ThumbnailService : MonoBehaviour
    {
	    private static RenderTexture temporaryThumbnailRenderTexture;
	    private static Texture2D temporaryThumbnailTexture;
	    private int width = 340;
	    private int height = 200;

	    private void Awake()
	    {
			temporaryThumbnailRenderTexture = new RenderTexture(width, height, 24);
		    temporaryThumbnailRenderTexture.Create();
		    temporaryThumbnailTexture = new Texture2D(temporaryThumbnailRenderTexture.width, temporaryThumbnailRenderTexture.height, TextureFormat.RGBA32, false);
	    }

        public Texture2D RenderThumbnail(Bounds targetBounds, bool orthographic = false)
		{
			float margin = 1.5f;
			float farClipPlaneCamera = 20000;
			Vector3 cameraRotation = new Vector3(60, 0, 0);
			
			var temporaryThumbnailCamera = new GameObject("ThumbnailCamera").AddComponent<Camera>();
			temporaryThumbnailCamera.orthographic = orthographic;
			temporaryThumbnailCamera.clearFlags = CameraClearFlags.Color;
			temporaryThumbnailCamera.backgroundColor = Color.grey;
			temporaryThumbnailCamera.enabled = false; // Only render on demand
			temporaryThumbnailCamera.farClipPlane = farClipPlaneCamera;
			temporaryThumbnailCamera.targetTexture = temporaryThumbnailRenderTexture;
			temporaryThumbnailCamera.cullingMask = ~((1 << 13) + (1 << 14)); // all layers except PolygonMask, PolygonMaskInverted		
            
			// Determine distance to cover bounds with camera
			var targetBoundsCenter = targetBounds.center;
			var targetBoundsSize = targetBounds.size;
			var targetBoundsMaxSize = Mathf.Max(targetBoundsSize.x, targetBoundsSize.y, targetBoundsSize.z);

			// Set camera in right angle; and move backwards to frame the target bounds
			temporaryThumbnailCamera.transform.position = targetBoundsCenter;
			temporaryThumbnailCamera.transform.eulerAngles = cameraRotation;
            temporaryThumbnailCamera.transform.Translate(Vector3.back * targetBoundsMaxSize * margin, Space.Self);
			temporaryThumbnailCamera.orthographicSize = targetBoundsMaxSize * 0.5f * margin;

			// add universal additional camera data, and set target renderer
			var additionalCameraData = temporaryThumbnailCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
			additionalCameraData.SetRenderer(2);

			// Render to our thumbnail texture
			temporaryThumbnailCamera.Render();
            temporaryThumbnailCamera.targetTexture = null;

            // Cleanup
            Destroy(temporaryThumbnailCamera.gameObject);
            
            RenderTexture.active = temporaryThumbnailRenderTexture;
            temporaryThumbnailTexture.ReadPixels(new Rect(0, 0, temporaryThumbnailRenderTexture.width, temporaryThumbnailRenderTexture.height), 0, 0);
            temporaryThumbnailTexture.Apply();
            RenderTexture.active = null;
            
            return temporaryThumbnailTexture;
		}
    }
}
