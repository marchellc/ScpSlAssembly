using System;
using CustomPlayerEffects;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106Attack : Scp106VigorAbilityBase
	{
		private Transform OwnerCam
		{
			get
			{
				return base.Owner.PlayerCameraReference;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		public static event Scp106Attack.PlayerTeleported OnPlayerTeleported;

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			float num = -1f;
			this._claimedOwnerPosition = base.CastRole.FpcModule.Position;
			this._claimedOwnerCamRotation = this.OwnerCam.rotation;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						Vector3 position = fpcRole.FpcModule.Position;
						Vector3 vector = position - this._claimedOwnerPosition;
						if (vector.sqrMagnitude <= this._maxRangeSqr)
						{
							float num2 = Vector3.Dot(vector.normalized, this.OwnerCam.forward);
							if (num2 >= num)
							{
								this._claimedTargetPosition = position;
								this._targetHub = referenceHub;
								num = num2;
							}
						}
					}
				}
			}
			if (num == -1f)
			{
				return;
			}
			base.ClientSendCmd();
		}

		private void SendCooldown(float cooldown)
		{
			if (cooldown <= 0f)
			{
				return;
			}
			this._nextAttack = NetworkTime.time + (double)cooldown;
			base.ServerSendRpc((ReferenceHub x) => x == base.Owner || base.Owner.IsSpectatedBy(x));
		}

		private void ReduceSinkholeCooldown()
		{
			base.CastRole.Sinkhole.ModifyCooldown(-5.0);
		}

		private bool VerifyShot()
		{
			IFpcRole fpcRole = this._targetHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			using (new FpcBacktracker(this._targetHub, this._claimedTargetPosition, 0.35f))
			{
				Vector3 position = base.CastRole.FpcModule.Position;
				Vector3 position2 = fpcRole.FpcModule.Position;
				Vector3 vector = position2 - position;
				float sqrMagnitude = vector.sqrMagnitude;
				if (sqrMagnitude > this._maxRangeSqr)
				{
					return false;
				}
				Vector3 forward = this.OwnerCam.forward;
				forward.y = 0f;
				vector.y = 0f;
				if (Physics.Linecast(position, position2, PlayerRolesUtils.BlockerMask))
				{
					return false;
				}
				if (this._dotOverDistance.Evaluate(sqrMagnitude) > Vector3.Dot(vector.normalized, forward.normalized))
				{
					this.SendCooldown(this._missCooldown);
					return false;
				}
			}
			return true;
		}

		private void ServerShoot()
		{
			if (!this.VerifyShot())
			{
				return;
			}
			PlayerEffectsController playerEffectsController = this._targetHub.playerEffectsController;
			Corroding effect = playerEffectsController.GetEffect<Corroding>();
			if (!playerEffectsController.GetEffect<Traumatized>().IsEnabled)
			{
				if (!effect.IsEnabled)
				{
					DamageHandlerBase damageHandlerBase = new ScpDamageHandler(base.Owner, (float)this._damage, DeathTranslations.PocketDecay);
					if (!this._targetHub.playerStats.DealDamage(damageHandlerBase))
					{
						return;
					}
					effect.AttackerHub = base.Owner;
					playerEffectsController.EnableEffect<Corroding>(20f, false);
				}
				else
				{
					Scp106TeleportingPlayerEvent scp106TeleportingPlayerEvent = new Scp106TeleportingPlayerEvent(base.Owner, this._targetHub);
					Scp106Events.OnTeleportingPlayer(scp106TeleportingPlayerEvent);
					if (!scp106TeleportingPlayerEvent.IsAllowed)
					{
						return;
					}
					Scp106Attack.PlayerTeleported onPlayerTeleported = Scp106Attack.OnPlayerTeleported;
					if (onPlayerTeleported != null)
					{
						onPlayerTeleported(base.Owner, this._targetHub);
					}
					playerEffectsController.EnableEffect<PocketCorroding>(0f, false);
					base.VigorAmount += 0.3f;
					Scp106Events.OnTeleportedPlayer(new Scp106TeleportedPlayerEvent(base.Owner, this._targetHub));
				}
				this.SendCooldown(this._hitCooldown);
				this.ReduceSinkholeCooldown();
				Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
				return;
			}
			DamageHandlerBase damageHandlerBase2 = new ScpDamageHandler(base.Owner, -1f, DeathTranslations.PocketDecay);
			if (!this._targetHub.playerStats.DealDamage(damageHandlerBase2))
			{
				return;
			}
			base.VigorAmount += 0.3f;
			this.SendCooldown(this._hitCooldown);
			this.ReduceSinkholeCooldown();
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteReferenceHub(this._targetHub);
			writer.WriteRelativePosition(new RelativePosition(this._claimedTargetPosition));
			writer.WriteQuaternion(this._claimedOwnerCamRotation);
			writer.WriteRelativePosition(new RelativePosition(this._claimedOwnerPosition));
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (this._nextAttack > NetworkTime.time)
			{
				return;
			}
			if (base.CastRole.Sinkhole.SubmergeProgress > 0f)
			{
				return;
			}
			this._targetHub = reader.ReadReferenceHub();
			this._claimedTargetPosition = reader.ReadRelativePosition().Position;
			this._claimedOwnerCamRotation = reader.ReadQuaternion();
			this._claimedOwnerPosition = reader.ReadRelativePosition().Position;
			if (this._targetHub == null || !HitboxIdentity.IsEnemy(base.Owner, this._targetHub))
			{
				return;
			}
			using (new FpcBacktracker(base.Owner, this._claimedOwnerPosition, this._claimedOwnerCamRotation, 0.1f, 0.15f))
			{
				this.ServerShoot();
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteDouble(this._nextAttack);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.ReduceSinkholeCooldown();
			Scp106Hud.PlayCooldownAnimation(reader.ReadDouble());
		}

		private const float TargetTraceTime = 0.35f;

		private const float VigorCaptureReward = 0.3f;

		private const float CooldownReductionReward = 5f;

		private const float CorrodingTime = 20f;

		private ReferenceHub _targetHub;

		private Quaternion _claimedOwnerCamRotation;

		private Vector3 _claimedOwnerPosition;

		private Vector3 _claimedTargetPosition;

		private double _nextAttack;

		[SerializeField]
		private AnimationCurve _dotOverDistance;

		[SerializeField]
		private float _maxRangeSqr;

		[SerializeField]
		private float _hitCooldown;

		[SerializeField]
		private float _missCooldown;

		[SerializeField]
		private int _damage;

		public delegate void PlayerTeleported(ReferenceHub scp106, ReferenceHub hub);
	}
}
