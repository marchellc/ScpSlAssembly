using System;
using CustomPlayerEffects;
using PlayerRoles.Visibility;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcVisibilityController : VisibilityController
	{
		protected virtual int NormalMaxRangeSqr
		{
			get
			{
				return 1300;
			}
		}

		protected virtual int SurfaceMaxRangeSqr
		{
			get
			{
				return 4900;
			}
		}

		public override InvisibilityFlags IgnoredFlags
		{
			get
			{
				InvisibilityFlags invisibilityFlags = base.IgnoredFlags;
				if (this._scp1344Effect.IsEnabled)
				{
					invisibilityFlags |= (InvisibilityFlags)7U;
				}
				return invisibilityFlags;
			}
		}

		public override InvisibilityFlags GetActiveFlags(ReferenceHub observer)
		{
			InvisibilityFlags invisibilityFlags = base.GetActiveFlags(observer);
			if (this._invisibleEffect.IsEnabled)
			{
				invisibilityFlags |= InvisibilityFlags.Scp268;
			}
			IFpcRole fpcRole = observer.roleManager.CurrentRole as IFpcRole;
			if (fpcRole != null)
			{
				IFpcRole fpcRole2 = base.Owner.roleManager.CurrentRole as IFpcRole;
				if (fpcRole2 != null)
				{
					Vector3 position = fpcRole.FpcModule.Position;
					Vector3 position2 = fpcRole2.FpcModule.Position;
					float num = (float)((Mathf.Min(position.y, position2.y) > 800f) ? this.SurfaceMaxRangeSqr : this.NormalMaxRangeSqr);
					if ((position - position2).sqrMagnitude > num)
					{
						invisibilityFlags |= InvisibilityFlags.OutOfRange;
					}
					return invisibilityFlags;
				}
			}
			return invisibilityFlags;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			PlayerEffectsController playerEffectsController = base.Owner.playerEffectsController;
			this._invisibleEffect = playerEffectsController.GetEffect<Invisible>();
			this._scp1344Effect = playerEffectsController.GetEffect<Scp1344>();
		}

		private const int SurfaceHeight = 800;

		private Invisible _invisibleEffect;

		private Scp1344 _scp1344Effect;
	}
}
