using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Usables.Scp1344;
using Mirror;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects;

public class Scp1344Detected : StatusEffectBase, ICustomRADisplay
{
	private readonly List<Scp1344HumanXrayProvider> _seenBy = new List<Scp1344HumanXrayProvider>();

	private float _remainingReEnableCooldown;

	[SerializeField]
	private AudioClip _detectedClip;

	[SerializeField]
	private float _reEnableCooldown;

	public string DisplayName => null;

	public bool CanBeDisplayed => false;

	public void ServerRegisterObserver(Scp1344HumanXrayProvider source)
	{
		if (!this._seenBy.Contains(source) && this.ValidateObserver(source))
		{
			this._seenBy.Add(source);
			this.UpdateStatus();
		}
	}

	protected override void Enabled()
	{
		base.Enabled();
		if (base.IsPOV && !(this._remainingReEnableCooldown > 0f))
		{
			AudioSourcePoolManager.Play2D(this._detectedClip, 1f, MixerChannel.NoDucking);
		}
	}

	protected override void Disabled()
	{
		base.Disabled();
		this._seenBy.Clear();
		this._remainingReEnableCooldown = this._reEnableCooldown;
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		this.UpdateStatus();
	}

	protected override void Update()
	{
		base.Update();
		if (this._remainingReEnableCooldown > 0f)
		{
			this._remainingReEnableCooldown -= Time.deltaTime;
		}
	}

	private bool ValidateObserver(Scp1344HumanXrayProvider source)
	{
		if (source != null && HitboxIdentity.IsEnemy(source.Hub, base.Hub))
		{
			return source.GetVisibilityForTarget(base.Hub);
		}
		return false;
	}

	private void UpdateStatus()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		for (int num = this._seenBy.Count - 1; num >= 0; num--)
		{
			if (!this.ValidateObserver(this._seenBy[num]))
			{
				this._seenBy.RemoveAt(num);
			}
		}
		base.IsEnabled = this._seenBy.Count > 0;
	}
}
