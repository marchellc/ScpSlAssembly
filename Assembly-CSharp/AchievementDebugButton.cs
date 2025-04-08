using System;
using Achievements;
using TMPro;
using UnityEngine;

public class AchievementDebugButton : MonoBehaviour
{
	public AchievementName TargetAchievement { get; set; }

	private Achievement Achievement
	{
		get
		{
			Achievement achievement;
			if (!AchievementManager.Achievements.TryGetValue(this.TargetAchievement, out achievement))
			{
				throw new NullReferenceException(string.Format("Could not find achievement by name {0}.", this.TargetAchievement));
			}
			return achievement;
		}
	}

	private bool IsUnlocked
	{
		get
		{
			Achievement achievement = this.Achievement;
			return !AchievementDebugMenu.QueuedAchievements.Contains(achievement);
		}
	}

	public void AddToQueue()
	{
		if (!this.IsUnlocked)
		{
			return;
		}
		AchievementDebugMenu.QueuedAchievements.Enqueue(this.Achievement);
		this.UpdateState();
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

	[SerializeField]
	private TMP_Text _stateText;

	[SerializeField]
	private TMP_Text _nameText;
}
