using System;
using GameCore;
using UnityEngine;

public class AchievementDebugOpener : MonoBehaviour
{
	private void Awake()
	{
		bool flag = global::GameCore.Version.BuildType == global::GameCore.Version.VersionType.Development || global::GameCore.Version.PublicBeta || global::GameCore.Version.PrivateBeta;
		this._buttonObj.SetActive(flag);
	}

	[SerializeField]
	private GameObject _buttonObj;
}
