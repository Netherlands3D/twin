using Netherlands3D.Twin.UI.Loader;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AsyncLoadingScreenSpawner : MonoBehaviour
{
    [SerializeField] private AsyncOperationLoadingScreen loadingScreenPrefab;
    
    // Cached instance
    private static AsyncLoadingScreenSpawner loadingScreenSpawner;

    public static AsyncLoadingScreenSpawner Instance()
    {
        return !loadingScreenSpawner ? FindObjectOfType<AsyncLoadingScreenSpawner>() : loadingScreenSpawner;
    }
    
    public void Spawn(string label, AsyncOperationHandle<GameObject> handle)
    {
        var loadingScreen = Instantiate(loadingScreenPrefab, transform);
        loadingScreen.Label = label;
        loadingScreen.Handle = handle;
    }
}
