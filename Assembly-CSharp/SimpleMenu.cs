using System;
using System.Collections.Generic;
using CentralAuth;
using MEC;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleMenu : MonoBehaviour
{
	private void Awake()
	{
		if (this.isPreloader)
		{
			return;
		}
		CentralAuthManager.InitAuth();
		foreach (string text in StartupArgs.Args)
		{
			if (!(text == "-fastmenu"))
			{
				if (!(text == "-newmenu"))
				{
					if (!(text == "-nographics"))
					{
						if (text == "-forcemenu")
						{
							SimpleMenu._forceSettings = true;
						}
					}
					else
					{
						SimpleMenu._server = true;
					}
				}
				else
				{
					PlayerPrefsSl.Set("menumode", 1);
				}
			}
			else
			{
				PlayerPrefsSl.Set("fastmenu", true);
				PlayerPrefsSl.Set("menumode", 2);
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
		if (!this.isPreloader)
		{
			yield break;
		}
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Loader", LoadSceneMode.Single);
		asyncOperation.allowSceneActivation = true;
		LauncherAssetScanProgressBar.Text = "LOADING";
		float sceneProgress = 0f;
		while (!asyncOperation.isDone)
		{
			sceneProgress = Math.Max(sceneProgress, asyncOperation.progress);
			LauncherAssetScanProgressBar.Progress = sceneProgress;
			yield return float.NegativeInfinity;
		}
		yield break;
	}

	public void ChangeMode()
	{
		PlayerPrefsSl.Set("fastmenu", false);
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
		SimpleMenu._targetSceneName = (SimpleMenu._server ? "FastMenu" : SimpleMenu.MenuSceneNames[SimpleMenu._forceSettings ? Mathf.Clamp(PlayerPrefsSl.Get("menumode", 1), 0, 2) : 1]);
		global::UnityEngine.Object.FindObjectOfType<CustomNetworkManager>().offlineScene = SimpleMenu._targetSceneName;
	}

	public static void LoadCorrectScene()
	{
		SceneManager.LoadScene(SimpleMenu._targetSceneName);
	}

	public bool isPreloader;

	private static bool _server;

	private static bool _forceSettings;

	private static string _targetSceneName;

	private const float minLoadingTime = 3f;

	internal static readonly string[] MenuSceneNames = new string[] { "MainMenuRemastered", "NewMainMenu", "FastMenu" };
}
