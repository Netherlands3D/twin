using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetCDFTest : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void LoadNetCDF();

    [DllImport("__Internal")]
    private static extern void TestNetCDF();
#endif

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        LoadNetCDF();
        StartCoroutine(WaitAndTest());
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    IEnumerator WaitAndTest()
    {
        // simple polling wait — swap for SendMessage callback later if you want something cleaner
        yield return new WaitForSeconds(0.5f);
        TestNetCDF();
    }
#endif
}