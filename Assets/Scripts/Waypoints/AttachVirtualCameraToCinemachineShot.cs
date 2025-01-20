using System.Collections;
using Cinemachine;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
namespace Netherlands3D.Twin
{
    public class AttachVirtualCameraToCinemachineShot : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Wait a frame due to the way layers work
            yield return null;
            
            var targetDisplayName = GetComponent<LayerGameObject>().LayerData.Name;
            var director = GameObject.Find("Waypoints").GetComponent<PlayableDirector>();
            
            Debug.Log(targetDisplayName);
            Debug.Log(director);
            if (director == null) yield break;
            
            Debug.Log(director.playableAsset);
            if (director.playableAsset is not TimelineAsset timeline) yield break;
            
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not CinemachineTrack) continue;
                foreach (var clip in track.GetClips())
                {
                    Debug.Log(clip.displayName);
                    Debug.Log(targetDisplayName);
                    Debug.Log(clip.displayName != targetDisplayName);
                    Debug.Log(clip.asset);
                    Debug.Log(clip.asset is not CinemachineShot);
                    
                    if (clip.displayName != targetDisplayName) continue;
                    if (clip.asset is not CinemachineShot shot) continue;
                    var virtualCamera = GetComponent<CinemachineVirtualCamera>();
                    var exposedReference = new ExposedReference<CinemachineVirtualCameraBase>
                    {
                        exposedName = targetDisplayName
                    };
                    director.SetReferenceValue(exposedReference.exposedName, virtualCamera);
                    Debug.Log("Setting Virtual camera of shot to exposedReference");
                    Debug.Log(shot);
                    Debug.Log(virtualCamera);
                    Debug.Log(exposedReference);
                    shot.VirtualCamera = exposedReference;
                }
            }
        }        
    }
}