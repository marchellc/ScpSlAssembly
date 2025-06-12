using System;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public abstract class Scp106VigorAbilityBase : KeySubroutine<Scp106Role>
{
	private VigorStat _vigor;

	private bool _vigorSet;

	public virtual bool ServerWantsSubmerged => false;

	public virtual float EmergeTime => 1.9f;

	public virtual float SubmergeTime => 3.1f;

	protected float VigorAmount
	{
		get
		{
			return this.Vigor.CurValue;
		}
		set
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Attempting to set Vigor amount as client.");
			}
			Scp106ChangingVigorEventArgs e = new Scp106ChangingVigorEventArgs(base.Owner, this.Vigor.CurValue, value);
			Scp106Events.OnChangingVigor(e);
			if (e.IsAllowed)
			{
				value = e.Value;
				this.Vigor.CurValue = Mathf.Clamp01(value);
				Scp106Events.OnChangedVigor(new Scp106ChangedVigorEventArgs(base.Owner, e.OldValue, value));
			}
		}
	}

	private VigorStat Vigor
	{
		get
		{
			if (this._vigorSet)
			{
				return this._vigor;
			}
			if (!base.Role.TryGetOwner(out var hub))
			{
				throw new InvalidOperationException("Attempting to access Vigor of inactive SCP-106.");
			}
			if (!hub.playerStats.TryGetModule<VigorStat>(out var module))
			{
				throw new InvalidOperationException("Vigor stat is not defined.");
			}
			this._vigor = module;
			this._vigorSet = true;
			return module;
		}
	}

	public override void ResetObject()
	{
		this._vigorSet = false;
	}
}
