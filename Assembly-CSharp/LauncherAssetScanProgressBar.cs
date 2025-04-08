using System;
using GameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LauncherAssetScanProgressBar : MonoBehaviour
{
	private void Awake()
	{
		this._version.text = this.GetVersionName();
	}

	private void Update()
	{
		this._throbber.fillAmount = LauncherAssetScanProgressBar.Progress;
		this._text.text = string.Format("{0}\n{1}%", LauncherAssetScanProgressBar.Text, Mathf.RoundToInt(LauncherAssetScanProgressBar.Progress * 100f));
	}

	private string GetVersionName()
	{
		string text;
		switch (global::GameCore.Version.BuildType)
		{
		case global::GameCore.Version.VersionType.PublicRC:
		case global::GameCore.Version.VersionType.PrivateRC:
		case global::GameCore.Version.VersionType.PrivateRCStreamingForbidden:
			text = "RELEASE CANDIDATE";
			break;
		case global::GameCore.Version.VersionType.PublicBeta:
			text = "PUBLIC BETA";
			break;
		case global::GameCore.Version.VersionType.PrivateBeta:
		case global::GameCore.Version.VersionType.PrivateBetaStreamingForbidden:
			text = "CLOSED BETA";
			break;
		case global::GameCore.Version.VersionType.Development:
			return "INTERNAL BUILD " + global::GameCore.Version.VersionString;
		default:
			text = global::GameCore.Version.BuildType.ToString().ToUpperInvariant();
			break;
		}
		string text2 = global::GameCore.Version.Major.ToString() + "." + global::GameCore.Version.Minor.ToString();
		if (global::GameCore.Version.Revision > 0)
		{
			text2 = text2 + "." + global::GameCore.Version.Revision.ToString();
		}
		return text + " " + text2;
	}

	[SerializeField]
	private Image _throbber;

	[SerializeField]
	private SimpleMenu _menu;

	[SerializeField]
	private TMP_Text _text;

	[SerializeField]
	private TMP_Text _version;

	public static float Progress;

	public static string Text;
}
