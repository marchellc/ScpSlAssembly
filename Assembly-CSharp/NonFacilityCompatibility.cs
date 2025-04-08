using System;
using PlayerRoles;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NonFacilityCompatibility : MonoBehaviour
{
	private void Awake()
	{
		NonFacilityCompatibility.singleton = this;
		SceneManager.sceneLoaded += NonFacilityCompatibility.RefreshDescription;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= NonFacilityCompatibility.RefreshDescription;
	}

	public static void RefreshDescription(Scene scene, LoadSceneMode mode)
	{
		foreach (NonFacilityCompatibility.SceneDescription sceneDescription in NonFacilityCompatibility.singleton.allScenes)
		{
			if (sceneDescription.sceneName == scene.name)
			{
				NonFacilityCompatibility.currentSceneSettings = sceneDescription;
			}
		}
	}

	public NonFacilityCompatibility.SceneDescription[] allScenes;

	public static NonFacilityCompatibility singleton;

	public static NonFacilityCompatibility.SceneDescription currentSceneSettings;

	[Serializable]
	public class SceneDescription
	{
		public string sceneName;

		public NonFacilityCompatibility.SceneDescription.VoiceChatSupportMode voiceChatSupport = NonFacilityCompatibility.SceneDescription.VoiceChatSupportMode.FullySupported;

		public bool enableWorldGeneration = true;

		public bool enableRespawning = true;

		public bool enableStandardGamplayItems = true;

		public bool roundAutostart;

		public Vector3 constantRespawnPoint = Vector3.zero;

		public RoleTypeId forcedClass = RoleTypeId.None;

		public enum VoiceChatSupportMode
		{
			Unsupported,
			WithoutIntercom,
			FullySupported
		}
	}
}
