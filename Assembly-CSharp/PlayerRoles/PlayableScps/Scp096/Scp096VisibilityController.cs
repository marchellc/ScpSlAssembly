using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Visibility;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096VisibilityController : FpcVisibilityController
	{
		public override InvisibilityFlags IgnoredFlags
		{
			get
			{
				InvisibilityFlags invisibilityFlags = base.IgnoredFlags;
				if (this.HideNonTargets)
				{
					invisibilityFlags |= (InvisibilityFlags)3U;
				}
				return invisibilityFlags;
			}
		}

		protected override int NormalMaxRangeSqr
		{
			get
			{
				return this.EnsureVisiblityForState(base.NormalMaxRangeSqr);
			}
		}

		protected override int SurfaceMaxRangeSqr
		{
			get
			{
				return this.EnsureVisiblityForState(base.SurfaceMaxRangeSqr);
			}
		}

		private bool HideNonTargets
		{
			get
			{
				return this._role.IsRageState(Scp096RageState.Distressed) || this._role.IsRageState(Scp096RageState.Enraged);
			}
		}

		private int EnsureVisiblityForState(int defaultRange)
		{
			Scp096AudioPlayer.Scp096StateAudio scp096StateAudio;
			if (!Scp096AudioPlayer.TryGetAudioForState(this._role.StateController.RageState, out scp096StateAudio))
			{
				return defaultRange;
			}
			float num = scp096StateAudio.MaxDistance + 10f;
			return Mathf.Max(Mathf.RoundToInt(num * num), defaultRange);
		}

		public override InvisibilityFlags GetActiveFlags(ReferenceHub observer)
		{
			return base.GetActiveFlags(observer);
		}

		public override bool ValidateVisibility(ReferenceHub target)
		{
			if (this.HideNonTargets)
			{
				return this._targetsTracker.HasTarget(target) && base.ValidateVisibility(target);
			}
			return base.ValidateVisibility(target);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._role = base.Role as Scp096Role;
			this._role.SubroutineModule.TryGetSubroutine<Scp096TargetsTracker>(out this._targetsTracker);
		}

		private const float RageRangeBuffer = 10f;

		private Scp096Role _role;

		private Scp096TargetsTracker _targetsTracker;
	}
}
