using PlayerRoles;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics;

public class InfluenceDisplay : SerializedWaveInterface
{
	[SerializeField]
	private TextMeshProUGUI _influence;

	protected override void Awake()
	{
		base.Awake();
		Faction targetFaction = base.Wave.TargetFaction;
		UpdateDisplay(targetFaction, FactionInfluenceManager.Get(targetFaction));
		FactionInfluenceManager.InfluenceModified += UpdateDisplay;
	}

	private void OnDestroy()
	{
		FactionInfluenceManager.InfluenceModified -= UpdateDisplay;
	}

	private void UpdateDisplay(Faction faction, float newValue)
	{
		if (base.Wave.TargetFaction == faction)
		{
			if (!RespawnTokensManager.TryGetNextThreshold(faction, newValue, out var threshold))
			{
				_influence.text = "MAX";
			}
			else
			{
				_influence.text = $"{newValue}/{threshold}";
			}
		}
	}
}
