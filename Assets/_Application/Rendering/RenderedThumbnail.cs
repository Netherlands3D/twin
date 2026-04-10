using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Rendering
{
	//TODO make this class a static helper class with only static methods after UIToolkit is fully implemented. No need to keep this as a monobehaviour.
	
    public class RenderedThumbnail : MonoBehaviour
    {
	    
	    
        [Header("Thumbnail")]
        [SerializeField] private RawImage thumbnail;
        [SerializeField] private GameObject displayIfNoThumbnail;

        [Tooltip("Extra space around the target bounds in the thumbnail")]
        [SerializeField] private float margin = 1.5f;
        [SerializeField] private Vector3 cameraRotation = new Vector3(60, 0, 0);

        [Tooltip("Check the UniversalRenderPipelineAsset.asset file for the renderer index you want to use")]

		[Header("Rendering")]
		[SerializeField] private int thumbnailRendererIndex = 2;
		[SerializeField] private bool orthographic = false;
		[SerializeField] private float farClipPlaneCamera = 20000;
		private RenderTexture thumbnailRenderTexture;
		private static RenderTexture temporaryThumbnailRenderTexture;

        /// <summary>
		/// Render world bounds to thumbnail
		/// </summary>
		/// <param name="targetBounds">The bounds object covering the camera target object in world space</param>
		public void RenderThumbnail(Bounds targetBounds)
		{
			if(thumbnailRenderTexture != null) Destroy(thumbnailRenderTexture);
			
            //Root canvas scale to make sure thumbnail rendered texture matches size on screen
            var scale = thumbnail.canvas.rootCanvas.transform.localScale;     
            var width = thumbnail.rectTransform.rect.width * scale.x;
            var height = thumbnail.rectTransform.rect.height * scale.y;
    
            // Create new rendertexture and camera
			thumbnailRenderTexture = new RenderTexture((int)width, (int)height, 24);
			thumbnailRenderTexture.Create();
			var temporaryThumbnailCamera = new GameObject("ThumbnailCamera").AddComponent<Camera>();
			temporaryThumbnailCamera.orthographic = orthographic;
			temporaryThumbnailCamera.clearFlags = CameraClearFlags.Color;
			temporaryThumbnailCamera.backgroundColor = Color.grey;
			temporaryThumbnailCamera.enabled = false; // Only render on demand
			temporaryThumbnailCamera.farClipPlane = farClipPlaneCamera;
			temporaryThumbnailCamera.targetTexture = thumbnailRenderTexture;
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
			additionalCameraData.SetRenderer(thumbnailRendererIndex);

			// Render to our thumbnail texture
			temporaryThumbnailCamera.Render();
            temporaryThumbnailCamera.targetTexture = null;

			// Set thumbnail texture to rawimage
			thumbnail.texture = thumbnailRenderTexture;

			if(displayIfNoThumbnail) displayIfNoThumbnail.SetActive(false);

            // Cleanup
            Destroy(temporaryThumbnailCamera.gameObject);
		}

		public void ClearRender()
		{
			if(thumbnailRenderTexture != null) Destroy(thumbnailRenderTexture);
			thumbnail.texture = null;

			if(displayIfNoThumbnail) displayIfNoThumbnail.SetActive(true);
		}

        private void OnDestroy() {
            if(thumbnailRenderTexture != null) Destroy(thumbnailRenderTexture);
        }
        
        
        //TODO this should be the only method to use when UIToolkit is done
        public static RenderTexture RenderThumbnail(Bounds targetBounds, bool orthographic = false, int width = 340, int height = 200)
		{
			if(temporaryThumbnailRenderTexture != null) Destroy(temporaryThumbnailRenderTexture);

			float margin = 1.5f;
			float farClipPlaneCamera = 20000;
			Vector3 cameraRotation = new Vector3(60, 0, 0);
    
            // Create new rendertexture and camera
            temporaryThumbnailRenderTexture = new RenderTexture(width, height, 24);
            temporaryThumbnailRenderTexture.Create();
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
            
            return temporaryThumbnailRenderTexture;
		}
    }
}
