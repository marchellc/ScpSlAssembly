using System;
using UnityEngine;

public class ReproProjectSettings : ScriptableObject
{
	public string ProjectName;

	public string ProjectPath;

	public bool OpenProject;

	public int TextureScale;

	public ReproProjectSettings.InputItem[] InputFiles;

	public ReproProjectSettings.InputItem[] ProjectFiles;

	[Serializable]
	public struct InputItem
	{
		public ReproProjectAssetType AssetType;

		public string AssetPath;
	}
}
