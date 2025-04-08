using System;
using PlayerRoles;
using TMPro;
using UnityEngine;

namespace Respawning.Graphics
{
	public class InfluenceDisplay : SerializedWaveInterface
	{
		protected override void Awake()
		{
			base.Awake();
			Faction targetFaction = base.Wave.TargetFaction;
			this.UpdateDisplay(targetFaction, FactionInfluenceManager.Get(targetFaction));
			FactionInfluenceManager.InfluenceModified += this.UpdateDisplay;
		}

		private void OnDestroy()
		{
			FactionInfluenceManager.InfluenceModified -= this.UpdateDisplay;
		}

		private void UpdateDisplay(Faction faction, float newValue)
		{
			if (base.Wave.TargetFaction != faction)
			{
				return;
			}
			int num;
			if (!RespawnTokensManager.TryGetNextThreshold(faction, newValue, out num))
			{
				this._influence.text = "MAX";
				return;
			}
			this._influence.text = string.Format("{0}/{1}", newValue, num);
		}

		[SerializeField]
		private TextMeshProUGUI _influence;
	}
}
