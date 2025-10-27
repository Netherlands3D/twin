using System.Collections;
using UnityEngine;
using TMPro;
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
			memoryOutputText.text = $"Heap size: {SystemInfo.systemMemorySize}MB GC: {ConvertBytesToMegabytes(System.GC.GetTotalMemory(false)):F2}MB";
		}

		/// <summary>
		/// Convert bytes to megabytes (easier to read)
		/// </summary>
		/// <param name="bytes">Number of bytes</param>
		/// <returns></returns>
		private double ConvertBytesToMegabytes(long bytes)
		{
			return (bytes / 1024f) / 1024f;
		}
	}
}