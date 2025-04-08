using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public readonly struct ShotBacktrackData
	{
		public double Age
		{
			get
			{
				return NetworkTime.time - this.CreationTimestamp;
			}
		}

		public void WriteSelf(NetworkWriter writer)
		{
			writer.WriteRelativePosition(this.RelativeOwnerPosition);
			writer.WriteQuaternion(this.RelativeOwnerRotation);
			writer.WriteReferenceHub(this.PrimaryTargetHub);
			writer.WriteRelativePosition(this.PrimaryTargetRelativePosition);
		}

		public void ProcessShot(Firearm firearm, Action<ReferenceHub> processingMethod)
		{
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(this.RelativeOwnerPosition.WaypointId, out waypointBase))
			{
				return;
			}
			Vector3 worldspacePosition = waypointBase.GetWorldspacePosition(this.RelativeOwnerPosition.Relative);
			Quaternion worldspaceRotation = waypointBase.GetWorldspaceRotation(this.RelativeOwnerRotation);
			using (new FpcBacktracker(firearm.Owner, worldspacePosition, worldspaceRotation, 0.1f, 0.15f))
			{
				if (this.HasPrimaryTarget)
				{
					using (new FpcBacktracker(this.PrimaryTargetHub, this.PrimaryTargetRelativePosition.Position, 0.4f))
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
			IFpcRole fpcRole = firearm.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
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
			HitboxIdentity hitboxIdentity;
			if (ShotBacktrackData.TryGetPrimaryTarget(firearm, out hitboxIdentity))
			{
				IFpcRole fpcRole2 = hitboxIdentity.TargetHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole2 != null)
				{
					this.HasPrimaryTarget = true;
					this.PrimaryTargetHub = hitboxIdentity.TargetHub;
					this.PrimaryTargetRelativePosition = new RelativePosition(fpcRole2.FpcModule.Position);
					return;
				}
			}
			this.HasPrimaryTarget = false;
			this.PrimaryTargetHub = null;
			this.PrimaryTargetRelativePosition = ShotBacktrackData.DefaultPos;
		}

		private static bool TryGetPrimaryTarget(Firearm firearm, out HitboxIdentity bestHitbox)
		{
			CachedLayerMask hitregMask = HitscanHitregModuleBase.HitregMask;
			uint netId = firearm.Owner.netId;
			float num = 0.5f;
			Transform playerCameraReference = firearm.Owner.PlayerCameraReference;
			Vector3 position = playerCameraReference.position;
			Vector3 forward = playerCameraReference.forward;
			RaycastHit raycastHit;
			if (Physics.Raycast(position, forward, out raycastHit, 150f, hitregMask) && raycastHit.collider.TryGetComponent<HitboxIdentity>(out bestHitbox))
			{
				return true;
			}
			bestHitbox = null;
			foreach (HitboxIdentity hitboxIdentity in HitboxIdentity.Instances)
			{
				if (hitboxIdentity.NetworkId != netId && Mathf.Abs(hitboxIdentity.CenterOfMass.y - position.y) <= 50f)
				{
					float num2 = Vector3.Dot(forward, (hitboxIdentity.CenterOfMass - position).normalized);
					if (num2 >= num)
					{
						num = num2;
						bestHitbox = hitboxIdentity;
					}
				}
			}
			return bestHitbox != null;
		}

		private static readonly RelativePosition DefaultPos;

		public readonly RelativePosition RelativeOwnerPosition;

		public readonly Quaternion RelativeOwnerRotation;

		public readonly bool HasPrimaryTarget;

		public readonly ReferenceHub PrimaryTargetHub;

		public readonly RelativePosition PrimaryTargetRelativePosition;

		public readonly double CreationTimestamp;
	}
}
