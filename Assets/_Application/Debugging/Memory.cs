using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Profiling;

namespace Netherlands3D.Twin.Debugging
{
	public class Memory : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI memoryOutputText;

		private void Awake()
		{
			if(!memoryOutputText)
				memoryOutputText = GetComponent<TextMeshProUGUI>();
		}

		private void OnEnable()
		{
			StartCoroutine(MemoryTick());
		}
		private void OnDisable()
		{
			StopAllCoroutines();
		}

		IEnumerator MemoryTick()
		{
			while (true)
			{
				DrawMemoryUsageInHeap();
				yield return new WaitForSeconds(0.5f);
			}
		}

		/// <summary>
		/// Draws the GC total memory as MB in the Text component
		/// </summary>
		private void DrawMemoryUsageInHeap()
		{
#if ENABLE_PROFILER
			memoryOutputText.text = $"Heap size: {ConvertBytesToMegabytes(Profiler.GetMonoUsedSizeLong()):F2}MB / {Profiler.GetMonoHeapSizeLong():F2}MB | GC: {ConvertBytesToMegabytes(System.GC.GetTotalMemory(false)):F2}MB";
#else
			memoryOutputText.text = $"Heap size: {SystemInfo.systemMemorySize:F2}MB | GC: {ConvertBytesToMegabytes(System.GC.GetTotalMemory(false)):F2}MB";
#endif
		}

		/// <summary>
		/// Convert bytes to megabytes (easier to read)
		/// </summary>
		/// <param name="bytes">Number of bytes</param>
		/// <returns></returns>
		private double ConvertBytesToMegabytes(long bytes)
		{
			return bytes / 1048576d;
		}
	}
}