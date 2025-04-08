using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173SnapAbility : KeySubroutine<Scp173Role>
	{
		private static int SnapMask
		{
			get
			{
				if (Scp173SnapAbility._snapMask == 0)
				{
					Scp173SnapAbility._snapMask = LayerMask.GetMask(new string[] { "Default", "Hitbox", "Glass", "Door" });
				}
				return Scp173SnapAbility._snapMask;
			}
		}

		public bool IsSpeeding
		{
			get
			{
				Scp173BreakneckSpeedsAbility scp173BreakneckSpeedsAbility;
				return base.CastRole.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out scp173BreakneckSpeedsAbility) && scp173BreakneckSpeedsAbility.IsActive;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (this.IsSpeeding)
			{
				return;
			}
			if (!Scp173SnapAbility.TryHitTarget(base.Owner.PlayerCameraReference, out this._targetHub))
			{
				return;
			}
			base.ClientSendCmd();
		}

		private static bool TryHitTarget(Transform origin, out ReferenceHub target)
		{
			target = null;
			RaycastHit raycastHit;
			if (!Physics.Raycast(origin.position, origin.forward, out raycastHit, 1.5f, Scp173SnapAbility.SnapMask))
			{
				return false;
			}
			IDestructible destructible;
			if (raycastHit.collider.TryGetComponent<IDestructible>(out destructible))
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if (hitboxIdentity != null)
				{
					target = hitboxIdentity.TargetHub;
					return HitboxIdentity.IsEnemy(Team.SCPs, target.GetTeam());
				}
			}
			return false;
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteReferenceHub(this._targetHub);
			writer.WriteRelativePosition(new RelativePosition(this._targetHub.transform.position));
			writer.WriteRelativePosition(new RelativePosition(base.Owner.transform.position));
			writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(base.Owner.PlayerCameraReference.rotation));
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._targetHub = reader.ReadReferenceHub();
			if (this._observersTracker.IsObserved)
			{
				return;
			}
			if (!(this._targetHub == null))
			{
				IFpcRole fpcRole = this._targetHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					if (this.IsSpeeding)
					{
						return;
					}
					FirstPersonMovementModule fpcModule = base.CastRole.FpcModule;
					FirstPersonMovementModule fpcModule2 = fpcRole.FpcModule;
					Transform playerCameraReference = base.Owner.PlayerCameraReference;
					Vector3 position = fpcModule2.Position;
					Vector3 position2 = fpcModule.Position;
					Quaternion rotation = playerCameraReference.rotation;
					fpcModule2.Position = fpcModule2.Tracer.GenerateBounds(0.4f, true).ClosestPoint(reader.ReadRelativePosition().Position);
					Bounds bounds = fpcModule.Tracer.GenerateBounds(0.1f, true);
					bounds.Encapsulate(fpcModule.Position + fpcModule.Motor.Velocity * 0.2f);
					fpcModule.Position = bounds.ClosestPoint(reader.ReadRelativePosition().Position);
					playerCameraReference.rotation = reader.ReadLowPrecisionQuaternion().Value;
					ReferenceHub referenceHub;
					if (Scp173SnapAbility.TryHitTarget(playerCameraReference, out referenceHub) && referenceHub.playerStats.DealDamage(base.CastRole.DamageHandler))
					{
						Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
						Scp173AudioPlayer scp173AudioPlayer;
						if (base.CastRole.SubroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out scp173AudioPlayer))
						{
							scp173AudioPlayer.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Snap);
						}
					}
					fpcModule2.Position = position;
					fpcModule.Position = position2;
					playerCameraReference.rotation = rotation;
					return;
				}
			}
		}

		private const float SnapRayDistance = 1.5f;

		private const float TargetBacktrackingTime = 0.4f;

		private const float OwnerBacktrackingTime = 0.1f;

		private const float ForetrackingTime = 0.2f;

		private Scp173ObserversTracker _observersTracker;

		private ReferenceHub _targetHub;

		private static int _snapMask;
	}
}
