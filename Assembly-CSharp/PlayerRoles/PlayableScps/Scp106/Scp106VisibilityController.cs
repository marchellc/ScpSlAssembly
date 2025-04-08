using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Visibility;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106VisibilityController : FpcVisibilityController
	{
		private bool CheckPlayer(ReferenceHub observer)
		{
			return !HitboxIdentity.IsEnemy(base.Owner, observer) || this._visSubroutine.SyncDamage.ContainsKey(observer.PlayerId);
		}

		public override InvisibilityFlags GetActiveFlags(ReferenceHub observer)
		{
			InvisibilityFlags invisibilityFlags = base.GetActiveFlags(observer);
			if (this._role106.Sinkhole.IsHidden && !this.CheckPlayer(observer))
			{
				invisibilityFlags |= InvisibilityFlags.Scp106Sinkhole;
			}
			return invisibilityFlags;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			if (!NetworkServer.active)
			{
				return;
			}
			this._role106 = base.Role as Scp106Role;
			this._role106.SubroutineModule.TryGetSubroutine<Scp106StalkVisibilityController>(out this._visSubroutine);
		}

		private Scp106Role _role106;

		private Scp106StalkVisibilityController _visSubroutine;
	}
}
