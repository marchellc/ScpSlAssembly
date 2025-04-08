using System;
using TMPro;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.HUDs
{
	public abstract class ScpHudBase : MonoBehaviour
	{
		public ReferenceHub Hub { get; private set; }

		public TMP_Text TargetCounter { get; private set; }

		public event Action OnDestroyed;

		protected virtual void ToggleHud(bool b)
		{
			Canvas[] componentsInChildren = base.GetComponentsInChildren<Canvas>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = b;
			}
		}

		protected virtual void Update()
		{
			if (!this._useCounter || (this._updateCounterTimer -= Time.deltaTime) > 0f)
			{
				return;
			}
			this.UpdateCounter();
		}

		protected virtual void OnDestroy()
		{
			if (!this._eventAssigned)
			{
				return;
			}
			HideHUDController.ToggleHUD -= this.ToggleHud;
			Action onDestroyed = this.OnDestroyed;
			if (onDestroyed == null)
			{
				return;
			}
			onDestroyed();
		}

		protected virtual void UpdateCounter()
		{
			int num = ReferenceHub.AllHubs.Count(delegate(ReferenceHub hub)
			{
				Faction faction = hub.GetFaction();
				return faction == Faction.FoundationStaff || faction == Faction.FoundationEnemy || faction == Faction.Flamingos;
			});
			int extraTargets = RoundSummary.singleton.ExtraTargets;
			this.ResetTimer(1f);
			this.TargetCounter.text = (num + extraTargets).ToString();
		}

		internal virtual void OnDied()
		{
		}

		internal virtual void Init(ReferenceHub hub)
		{
			this.Hub = hub;
			this._eventAssigned = true;
			this._useCounter = this.TargetCounter != null;
			HideHUDController.ToggleHUD += this.ToggleHud;
			if (HideHUDController.IsHUDVisible)
			{
				return;
			}
			this.ToggleHud(false);
		}

		protected void ResetTimer(float delayInSeconds = 1f)
		{
		}

		private float _updateCounterTimer;

		private bool _eventAssigned;

		private bool _useCounter;
	}
}
