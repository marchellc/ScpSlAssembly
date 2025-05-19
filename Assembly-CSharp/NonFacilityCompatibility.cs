using System;
using PlayerRoles;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonFacilityCompatibility : MonoBehaviour
{
	[Serializable]
	public class SceneDescription
	{
		public enum VoiceChatSupportMode
		{
			Unsupported,
			WithoutIntercom,
			FullySupported
		}

		public string sceneName;

		public VoiceChatSupportMode voiceChatSupport = VoiceChatSupportMode.FullySupported;

		public bool enableWorldGeneration = true;

		public bool enableRespawning = true;

		public bool enableStandardGamplayItems = true;

		public bool roundAutostart;

		public Vector3 constantRespawnPoint = Vector3.zero;

		public RoleTypeId forcedClass = RoleTypeId.None;
	}

	public SceneDescription[] allScenes;

	public static NonFacilityCompatibility singleton;

	public static SceneDescription currentSceneSettings;

	private void Awake()
	{
		singleton = this;
		SceneManager.sceneLoaded += RefreshDescription;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= RefreshDescription;
	}

	public static void RefreshDescription(Scene scene, LoadSceneMode mode)
	{
		SceneDescription[] array = singleton.allScenes;
		foreach (SceneDescription sceneDescription in array)
		{
			if (sceneDescription.sceneName == scene.name)
			{
				currentSceneSettings = sceneDescription;
			}
		}
	}
}
