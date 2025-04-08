using System;
using System.Collections.Generic;
using Achievements;
using UnityEngine;

public class AchievementDebugMenu : MonoBehaviour
{
	public void ResetQueuedAchievements()
	{
		for (int i = 0; i < AchievementDebugMenu.QueuedAchievements.Count; i++)
		{
			AchievementDebugMenu.QueuedAchievements.Dequeue().Reset();
		}
	}

	private void Awake()
	{
		foreach (AchievementName achievementName in this.AllowedAchievements)
		{
			global::UnityEngine.Object.Instantiate<AchievementDebugButton>(this._buttonPrefab, this._targetParent).TargetAchievement = achievementName;
		}
	}

	private void OnDisable()
	{
		AchievementDebugMenu.QueuedAchievements.Clear();
	}

	public static readonly Queue<Achievement> QueuedAchievements = new Queue<Achievement>();

	[HideInInspector]
	public List<AchievementName> AllowedAchievements = new List<AchievementName>();

	[SerializeField]
	private AchievementDebugButton _buttonPrefab;

	[SerializeField]
	private Transform _targetParent;
}
