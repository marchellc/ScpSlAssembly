using System;
using TMPro;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.HUDs;

public abstract class ScpHudBase : MonoBehaviour
{
	private float _updateCounterTimer;

	private bool _eventAssigned;

	private bool _useCounter;

	public ReferenceHub Hub { get; private set; }

	[field: SerializeField]
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
		if (this._useCounter && !((this._updateCounterTimer -= Time.deltaTime) > 0f))
		{
			this.UpdateCounter();
		}
	}

	protected virtual void OnDestroy()
	{
		if (this._eventAssigned)
		{
			HideHUDController.ToggleHUD -= ToggleHud;
			this.OnDestroyed?.Invoke();
		}
	}

	protected virtual void UpdateCounter()
	{
		int num = ReferenceHub.AllHubs.Count(delegate(ReferenceHub hub)
		{
			Faction faction = hub.GetFaction();
			return faction == Faction.FoundationStaff || faction == Faction.FoundationEnemy || faction == Faction.Flamingos;
		});
		int extraTargets = RoundSummary.singleton.ExtraTargets;
		this.ResetTimer();
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
		HideHUDController.ToggleHUD += ToggleHud;
		if (!HideHUDController.IsHUDVisible)
		{
			this.ToggleHud(b: false);
		}
	}

	protected void ResetTimer(float delayInSeconds = 1f)
	{
	}
}
