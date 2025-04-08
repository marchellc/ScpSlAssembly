using System;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class DoorRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			DoorEvents.OnDoorAction += this.OnDoorAction;
			if (!NetworkServer.active)
			{
				return;
			}
			this._rippleAssigned = base.CastRole.SubroutineModule.TryGetSubroutine<SurfaceRippleTrigger>(out this._surfaceRippleTrigger);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			DoorEvents.OnDoorAction -= this.OnDoorAction;
			if (!NetworkServer.active)
			{
				return;
			}
			this._rippleAssigned = false;
			this._surfaceRippleTrigger = null;
		}

		private void OnDoorAction(DoorVariant dv, DoorAction da, ReferenceHub hub)
		{
			if (da != DoorAction.Closed && da != DoorAction.Opened)
			{
				return;
			}
			if (base.IsLocalOrSpectated || NetworkServer.active)
			{
				BasicDoor basicDoor = dv as BasicDoor;
				if (basicDoor != null)
				{
					float sqrMagnitude = (dv.transform.position + DoorRippleTrigger.PosOffset - base.CastRole.FpcModule.Position).sqrMagnitude;
					float num = basicDoor.MainSource.maxDistance * basicDoor.MainSource.maxDistance;
					if (sqrMagnitude > num)
					{
						return;
					}
					if (NetworkServer.active && this._rippleAssigned && hub != null && HitboxIdentity.IsEnemy(base.Owner, hub))
					{
						this._surfaceRippleTrigger.ProcessRipple(hub);
						return;
					}
					base.Player.Play(dv.transform.position + DoorRippleTrigger.PosOffset, Color.red);
					return;
				}
			}
		}

		private static readonly Vector3 PosOffset = Vector3.up * 1.25f;

		private SurfaceRippleTrigger _surfaceRippleTrigger;

		private bool _rippleAssigned;
	}
}
