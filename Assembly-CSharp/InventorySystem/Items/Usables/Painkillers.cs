using System;
using UnityEngine;

namespace InventorySystem.Items.Usables
{
	public class Painkillers : Consumable
	{
		protected override void OnEffectsActivated()
		{
			base.ServerAddRegeneration(this._healProgress, 0.06666667f, 50f);
			base.Owner.playerEffectsController.UseMedicalItem(this);
		}

		[SerializeField]
		private AnimationCurve _healProgress;

		private const float TotalRegenerationTime = 15f;

		private const int TotalHpToRegenerate = 50;
	}
}
