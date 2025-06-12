using System.Collections.Generic;
using Achievements;
using UnityEngine;

public class AchievementDebugMenu : MonoBehaviour
{
	public static readonly Queue<Achievement> QueuedAchievements = new Queue<Achievement>();

	[HideInInspector]
	public List<AchievementName> AllowedAchievements = new List<AchievementName>();

	[SerializeField]
	private AchievementDebugButton _buttonPrefab;

	[SerializeField]
	private Transform _targetParent;

	public void ResetQueuedAchievements()
	{
		for (int i = 0; i < AchievementDebugMenu.QueuedAchievements.Count; i++)
		{
			AchievementDebugMenu.QueuedAchievements.Dequeue().Reset();
		}
	}

	private void Awake()
	{
		foreach (AchievementName allowedAchievement in this.AllowedAchievements)
		{
			Object.Instantiate(this._buttonPrefab, this._targetParent).TargetAchievement = allowedAchievement;
		}
	}

	private void OnDisable()
	{
		AchievementDebugMenu.QueuedAchievements.Clear();
	}
}
