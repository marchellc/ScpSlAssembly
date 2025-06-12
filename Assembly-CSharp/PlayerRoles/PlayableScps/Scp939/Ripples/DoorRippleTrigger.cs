using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class DoorRippleTrigger : RippleTriggerBase
{
	private static readonly Vector3 PosOffset = Vector3.up * 1.25f;

	private SurfaceRippleTrigger _surfaceRippleTrigger;

	private bool _rippleAssigned;

	public override void SpawnObject()
	{
		base.SpawnObject();
		DoorEvents.OnDoorAction += OnDoorAction;
		if (NetworkServer.active)
		{
			this._rippleAssigned = base.CastRole.SubroutineModule.TryGetSubroutine<SurfaceRippleTrigger>(out this._surfaceRippleTrigger);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		DoorEvents.OnDoorAction -= OnDoorAction;
		if (NetworkServer.active)
		{
			this._rippleAssigned = false;
			this._surfaceRippleTrigger = null;
		}
	}

	private void OnDoorAction(DoorVariant dv, DoorAction da, ReferenceHub hub)
	{
		if ((da != DoorAction.Closed && da != DoorAction.Opened) || (!base.IsLocalOrSpectated && !NetworkServer.active) || !(dv is BasicDoor basicDoor))
		{
			return;
		}
		float sqrMagnitude = (dv.transform.position + DoorRippleTrigger.PosOffset - base.CastRole.FpcModule.Position).sqrMagnitude;
		float num = basicDoor.MainSource.maxDistance * basicDoor.MainSource.maxDistance;
		if (!(sqrMagnitude > num))
		{
			if (NetworkServer.active && this._rippleAssigned && hub != null && HitboxIdentity.IsEnemy(base.Owner, hub))
			{
				this._surfaceRippleTrigger.ProcessRipple(hub);
			}
			else
			{
				base.Player.Play(dv.transform.position + DoorRippleTrigger.PosOffset, Color.red);
			}
		}
	}
}
