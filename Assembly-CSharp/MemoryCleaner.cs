using System;
using System.Runtime;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MemoryCleaner
{
	[RuntimeInitializeOnLoadMethod]
	private static void OnLoad()
	{
		SceneManager.sceneLoaded += MemoryCleaner.CleanupMemory;
	}

	private static void CleanupMemory(Scene scene, LoadSceneMode mode)
	{
		Resources.UnloadUnusedAssets();
		new Thread(delegate
		{
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		})
		{
			IsBackground = true
		}.Start();
	}
}
