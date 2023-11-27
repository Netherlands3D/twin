using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class RenderedThumbnail : MonoBehaviour
    {
        [Header("Thumbnail")]
		[SerializeField] private UniversalRenderPipelineAsset thumbnailRendererOverride;
        [SerializeField] private RawImage thumbnail;

        [Tooltip("Extra space around the target bounds in the thumbnail")]
        [SerializeField] private float margin = 1.5f;
        [SerializeField] private Vector3 cameraRotation = new Vector3(60, 0, 0);

        [Tooltip("Check the UniversalRenderPipelineAsset.asset file for the renderer index you want to use")]
		[SerializeField] private int thumbnailRendererIndex = 2;
		[SerializeField] private bool orthographic = false;
		private RenderTexture thumbnailRenderTexture;

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
			temporaryThumbnailCamera.targetTexture = thumbnailRenderTexture;
			
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

            // Cleanup
            Destroy(temporaryThumbnailCamera.gameObject);
		}

        private void OnDestroy() {
            if(thumbnailRenderTexture != null) Destroy(thumbnailRenderTexture);
        }
    }
}
