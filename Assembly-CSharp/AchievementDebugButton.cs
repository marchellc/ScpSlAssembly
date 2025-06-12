using System;
using Achievements;
using TMPro;
using UnityEngine;

public class AchievementDebugButton : MonoBehaviour
{
	[SerializeField]
	private TMP_Text _stateText;

	[SerializeField]
	private TMP_Text _nameText;

	public AchievementName TargetAchievement { get; set; }

	private Achievement Achievement
	{
		get
		{
			if (!AchievementManager.Achievements.TryGetValue(this.TargetAchievement, out var value))
			{
				throw new NullReferenceException($"Could not find achievement by name {this.TargetAchievement}.");
			}
			return value;
		}
	}

	private bool IsUnlocked
	{
		get
		{
			Achievement achievement = this.Achievement;
			if (AchievementDebugMenu.QueuedAchievements.Contains(achievement))
			{
				return false;
			}
			return true;
		}
	}

	public void AddToQueue()
	{
		if (this.IsUnlocked)
		{
			AchievementDebugMenu.QueuedAchievements.Enqueue(this.Achievement);
			this.UpdateState();
		}
	}

	private void Start()
	{
		this._nameText.text = this.TargetAchievement.ToString();
		this.UpdateState();
	}

	private void OnEnable()
	{
		this.UpdateState();
	}

	private void OnDisable()
	{
		this.UpdateState();
	}

	private void UpdateState()
	{
		bool isUnlocked = this.IsUnlocked;
		this._stateText.text = (isUnlocked ? "✔" : "X");
		this._stateText.color = (isUnlocked ? Color.green : Color.red);
	}
}
