using System;
using System.Collections.Generic;
using CentralAuth;
using MEC;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleMenu : MonoBehaviour
{
	public bool isPreloader;

	private static bool _server;

	private static bool _forceSettings;

	private static string _targetSceneName;

	private const float minLoadingTime = 3f;

	internal static readonly string[] MenuSceneNames = new string[3] { "MainMenuRemastered", "NewMainMenu", "FastMenu" };

	private void Awake()
	{
		if (this.isPreloader)
		{
			return;
		}
		CentralAuthManager.InitAuth();
		string[] args = StartupArgs.Args;
		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
			case "-fastmenu":
				PlayerPrefsSl.Set("fastmenu", value: true);
				PlayerPrefsSl.Set("menumode", 2);
				break;
			case "-newmenu":
				PlayerPrefsSl.Set("menumode", 1);
				break;
			case "-nographics":
				SimpleMenu._server = true;
				break;
			case "-forcemenu":
				SimpleMenu._forceSettings = true;
				break;
			}
		}
		SimpleMenu.Refresh();
	}

	private void Start()
	{
		Timing.RunCoroutine(this.StartLoad());
	}

	private IEnumerator<float> StartLoad()
	{
		yield return float.NegativeInfinity;
		if (this.isPreloader)
		{
			AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Loader", LoadSceneMode.Single);
			asyncOperation.allowSceneActivation = true;
			LauncherAssetScanProgressBar.Text = "LOADING";
			float sceneProgress = 0f;
			while (!asyncOperation.isDone)
			{
				sceneProgress = (LauncherAssetScanProgressBar.Progress = Math.Max(sceneProgress, asyncOperation.progress));
				yield return float.NegativeInfinity;
			}
		}
	}

	public void ChangeMode()
	{
		PlayerPrefsSl.Set("fastmenu", value: false);
		PlayerPrefsSl.Set("menumode", 1);
		SimpleMenu.Refresh();
		SimpleMenu.LoadCorrectScene();
	}

	public static void ChangeMode(int id)
	{
		PlayerPrefsSl.Set("menumode", id);
		SimpleMenu.Refresh();
		SimpleMenu.LoadCorrectScene();
	}

	private static void Refresh()
	{
		SimpleMenu._targetSceneName = (SimpleMenu._server ? "FastMenu" : SimpleMenu.MenuSceneNames[(!SimpleMenu._forceSettings) ? 1 : Mathf.Clamp(PlayerPrefsSl.Get("menumode", 1), 0, 2)]);
		UnityEngine.Object.FindObjectOfType<CustomNetworkManager>().offlineScene = SimpleMenu._targetSceneName;
	}

	public static void LoadCorrectScene()
	{
		SceneManager.LoadScene(SimpleMenu._targetSceneName);
	}
}
