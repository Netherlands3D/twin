using Cinemachine;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace Netherlands3D.Twin
{
    public class AttachVirtualCameraToCinemachineShot : MonoBehaviour
    {
        private void OnEnable()
        {
            var targetDisplayName = GetComponent<LayerGameObject>().LayerData.Name;
            var director = GameObject.Find("Waypoints").GetComponent<PlayableDirector>();
            if (director == null)
            {
                return;
            }
            if (director.playableAsset is not TimelineAsset timeline) return;
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not CinemachineTrack) continue;
                foreach (var clip in track.GetClips())
                {
                    if (clip.displayName != targetDisplayName) continue;
                    if (clip.asset is not CinemachineShot shot) continue;
                    var virtualCamera = GetComponent<CinemachineVirtualCamera>();
                    var exposedReference = new ExposedReference<CinemachineVirtualCameraBase>
                    {
                        exposedName = virtualCamera.name
                    };
                    director.SetReferenceValue(exposedReference.exposedName, virtualCamera);
                    shot.VirtualCamera = exposedReference;
                }
            }
        }        
    }
}