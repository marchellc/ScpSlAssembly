using System;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public abstract class Scp106VigorAbilityBase : KeySubroutine<Scp106Role>
	{
		public virtual bool ServerWantsSubmerged
		{
			get
			{
				return false;
			}
		}

		public virtual float EmergeTime
		{
			get
			{
				return 1.9f;
			}
		}

		public virtual float SubmergeTime
		{
			get
			{
				return 3.1f;
			}
		}

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
				Scp106ChangingVigorEventArgs scp106ChangingVigorEventArgs = new Scp106ChangingVigorEventArgs(base.Owner, this.Vigor.CurValue, value);
				Scp106Events.OnChangingVigor(scp106ChangingVigorEventArgs);
				if (!scp106ChangingVigorEventArgs.IsAllowed)
				{
					return;
				}
				value = scp106ChangingVigorEventArgs.Value;
				this.Vigor.CurValue = Mathf.Clamp01(value);
				Scp106Events.OnChangedVigor(new Scp106ChangedVigorEventArgs(base.Owner, scp106ChangingVigorEventArgs.OldValue, value));
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
				ReferenceHub referenceHub;
				if (!base.Role.TryGetOwner(out referenceHub))
				{
					throw new InvalidOperationException("Attempting to access Vigor of inactive SCP-106.");
				}
				VigorStat vigorStat;
				if (!referenceHub.playerStats.TryGetModule<VigorStat>(out vigorStat))
				{
					throw new InvalidOperationException("Vigor stat is not defined.");
				}
				this._vigor = vigorStat;
				this._vigorSet = true;
				return vigorStat;
			}
		}

		public override void ResetObject()
		{
			this._vigorSet = false;
		}

		private VigorStat _vigor;

		private bool _vigorSet;
	}
}
