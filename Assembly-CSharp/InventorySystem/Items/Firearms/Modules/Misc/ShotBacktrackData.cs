using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public readonly struct ShotBacktrackData
{
	private static readonly RelativePosition DefaultPos;

	public readonly RelativePosition RelativeOwnerPosition;

	public readonly Quaternion RelativeOwnerRotation;

	public readonly bool HasPrimaryTarget;

	public readonly ReferenceHub PrimaryTargetHub;

	public readonly RelativePosition PrimaryTargetRelativePosition;

	public readonly double CreationTimestamp;

	public double Age => NetworkTime.time - this.CreationTimestamp;

	public void WriteSelf(NetworkWriter writer)
	{
		writer.WriteRelativePosition(this.RelativeOwnerPosition);
		writer.WriteQuaternion(this.RelativeOwnerRotation);
		writer.WriteReferenceHub(this.PrimaryTargetHub);
		writer.WriteRelativePosition(this.PrimaryTargetRelativePosition);
	}

	public void ProcessShot(Firearm firearm, Action<ReferenceHub> processingMethod)
	{
		if (!WaypointBase.TryGetWaypoint(this.RelativeOwnerPosition.WaypointId, out var wp))
		{
			return;
		}
		Vector3 worldspacePosition = wp.GetWorldspacePosition(this.RelativeOwnerPosition.Relative);
		Quaternion worldspaceRotation = wp.GetWorldspaceRotation(this.RelativeOwnerRotation);
		using (new FpcBacktracker(firearm.Owner, worldspacePosition, worldspaceRotation))
		{
			if (this.HasPrimaryTarget)
			{
				using (new FpcBacktracker(this.PrimaryTargetHub, this.PrimaryTargetRelativePosition.Position))
				{
					processingMethod(this.PrimaryTargetHub);
					return;
				}
			}
			processingMethod(null);
		}
	}

	public ShotBacktrackData(NetworkReader reader)
	{
		this.CreationTimestamp = NetworkTime.time;
		this.RelativeOwnerPosition = reader.ReadRelativePosition();
		this.RelativeOwnerRotation = reader.ReadQuaternion();
		this.HasPrimaryTarget = reader.TryReadReferenceHub(out this.PrimaryTargetHub);
		this.PrimaryTargetRelativePosition = reader.ReadRelativePosition();
	}

	public ShotBacktrackData(Firearm firearm)
	{
		this.CreationTimestamp = NetworkTime.time;
		if (!(firearm.Owner.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			this.RelativeOwnerPosition = ShotBacktrackData.DefaultPos;
			this.RelativeOwnerRotation = Quaternion.identity;
			this.HasPrimaryTarget = false;
			this.PrimaryTargetHub = null;
			this.PrimaryTargetRelativePosition = ShotBacktrackData.DefaultPos;
			return;
		}
		this.RelativeOwnerPosition = new RelativePosition(fpcRole.FpcModule.Position);
		this.RelativeOwnerRotation = WaypointBase.GetRelativeRotation(this.RelativeOwnerPosition.WaypointId, firearm.Owner.PlayerCameraReference.rotation);
		if (ShotBacktrackData.TryGetPrimaryTarget(firearm, out var bestHitbox) && bestHitbox.TargetHub.roleManager.CurrentRole is IFpcRole fpcRole2)
		{
			this.HasPrimaryTarget = true;
			this.PrimaryTargetHub = bestHitbox.TargetHub;
			this.PrimaryTargetRelativePosition = new RelativePosition(fpcRole2.FpcModule.Position);
		}
		else
		{
			this.HasPrimaryTarget = false;
			this.PrimaryTargetHub = null;
			this.PrimaryTargetRelativePosition = ShotBacktrackData.DefaultPos;
		}
	}

	private static bool TryGetPrimaryTarget(Firearm firearm, out HitboxIdentity bestHitbox)
	{
		CachedLayerMask hitregMask = HitscanHitregModuleBase.HitregMask;
		uint netId = firearm.Owner.netId;
		float num = 0.5f;
		Transform playerCameraReference = firearm.Owner.PlayerCameraReference;
		Vector3 position = playerCameraReference.position;
		Vector3 forward = playerCameraReference.forward;
		if (Physics.Raycast(position, forward, out var hitInfo, 150f, hitregMask) && hitInfo.collider.TryGetComponent<HitboxIdentity>(out bestHitbox))
		{
			return true;
		}
		bestHitbox = null;
		foreach (HitboxIdentity instance in HitboxIdentity.Instances)
		{
			if (instance.NetworkId != netId && !(Mathf.Abs(instance.CenterOfMass.y - position.y) > 50f))
			{
				float num2 = Vector3.Dot(forward, (instance.CenterOfMass - position).normalized);
				if (!(num2 < num))
				{
					num = num2;
					bestHitbox = instance;
				}
			}
		}
		return bestHitbox != null;
	}
}
