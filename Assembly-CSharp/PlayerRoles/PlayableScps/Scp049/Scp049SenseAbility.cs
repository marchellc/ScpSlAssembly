using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049SenseAbility : KeySubroutine<Scp049Role>
	{
		public event Action OnFailed;

		public event Action OnSuccess;

		public ReferenceHub Target { get; private set; }

		public bool HasTarget { get; private set; }

		public float DistanceFromTarget { get; private set; }

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.ToggleFlashlight;
			}
		}

		private bool CanSeeIndicator
		{
			get
			{
				return base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated() || ReferenceHub.LocalHub.GetRoleId() == RoleTypeId.Scp0492;
			}
		}

		public void ServerLoseTarget()
		{
			this.HasTarget = false;
			this.Cooldown.Trigger(20.0);
			base.ServerSendRpc(true);
		}

		public void ServerProcessKilledPlayer(ReferenceHub hub)
		{
			if (!this.HasTarget || this.Target != hub)
			{
				return;
			}
			this.DeadTargets.Add(hub);
			this.SpecialZombies.Add(hub);
			this.Cooldown.Trigger(40.0);
			this.HasTarget = false;
			base.ServerSendRpc(true);
		}

		public bool IsTarget(ReferenceHub hub)
		{
			return this.HasTarget && hub == this.Target;
		}

		protected override void Update()
		{
			base.Update();
			if (!this.HasTarget)
			{
				return;
			}
			bool flag = false;
			IFpcRole fpcRole = this.Target.roleManager.CurrentRole as IFpcRole;
			if (fpcRole != null)
			{
				flag = true;
				Vector3 position = fpcRole.FpcModule.Position;
				Vector3 position2 = base.CastRole.FpcModule.Position;
				this.DistanceFromTarget = (position - position2).sqrMagnitude;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, this.Target))
			{
				flag = false;
			}
			if (!NetworkServer.active)
			{
				return;
			}
			if (base.CastRole.VisibilityController.ValidateVisibility(this.Target) && !this.Duration.IsReady && flag)
			{
				return;
			}
			this.ServerLoseTarget();
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this.Duration.IsReady || !this.Cooldown.IsReady)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!this.CanFindTarget(out referenceHub))
			{
				this.Target = null;
				Action onFailed = this.OnFailed;
				if (onFailed != null)
				{
					onFailed();
				}
				base.ClientSendCmd();
				return;
			}
			Action onSuccess = this.OnSuccess;
			if (onSuccess != null)
			{
				onSuccess();
			}
			this.Target = referenceHub;
			base.ClientSendCmd();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			base.GetSubroutine<Scp049AttackAbility>(out this._attackAbility);
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			SpectatorTargetTracker.OnTargetChanged += this.OnSpectatorTargetChanged;
			this._attackAbility.OnServerHit += this.OnServerHit;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
			this.Duration.Clear();
			this.DeadTargets.Clear();
			this.HasTarget = false;
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			SpectatorTargetTracker.OnTargetChanged -= this.OnSpectatorTargetChanged;
			this._attackAbility.OnServerHit -= this.OnServerHit;
		}

		private void OnServerHit(ReferenceHub hub)
		{
			if (!this.HasTarget || hub == this.Target)
			{
				return;
			}
			this.ServerLoseTarget();
		}

		private void OnSpectatorTargetChanged()
		{
			if (!this._hasPulseUnsafe)
			{
				return;
			}
			if (this._pulseEffect != null)
			{
				global::UnityEngine.Object.Destroy(this._pulseEffect.gameObject);
			}
			this._hasPulseUnsafe = false;
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (newRole is HumanRole || newRole is ZombieRole)
			{
				this.DeadTargets.Remove(userHub);
			}
			if (prevRole is SpectatorRole && !(newRole is ZombieRole))
			{
				this.SpecialZombies.Remove(userHub);
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			if (!this.Cooldown.IsReady || !this.Duration.IsReady)
			{
				return;
			}
			this.HasTarget = false;
			this.Target = reader.ReadReferenceHub();
			if (this.Target == null)
			{
				this.Cooldown.Trigger(2.5);
				base.ServerSendRpc(true);
				return;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, this.Target))
			{
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = this.Target.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return;
			}
			float radius = fpcStandardRoleBase.FpcModule.CharController.radius;
			Vector3 cameraPosition = fpcStandardRoleBase.CameraPosition;
			if (!VisionInformation.GetVisionInformation(base.Owner, base.Owner.PlayerCameraReference, cameraPosition, radius, this._distanceThreshold, true, true, 0, false).IsLooking)
			{
				return;
			}
			Scp049UsingSenseEventArgs scp049UsingSenseEventArgs = new Scp049UsingSenseEventArgs(base.Owner, this.Target);
			Scp049Events.OnUsingSense(scp049UsingSenseEventArgs);
			if (!scp049UsingSenseEventArgs.IsAllowed)
			{
				return;
			}
			this.Target = scp049UsingSenseEventArgs.Target.ReferenceHub;
			this.Duration.Trigger(20.0);
			this.HasTarget = true;
			base.ServerSendRpc(true);
			Scp049Events.OnUsedSense(new Scp049UsedSenseEventArgs(base.Owner, this.Target));
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			writer.WriteReferenceHub(this.HasTarget ? this.Target : null);
			this.Cooldown.WriteCooldown(writer);
			this.Duration.WriteCooldown(writer);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			writer.WriteReferenceHub(this.Target);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			this.Target = reader.ReadReferenceHub();
			this.HasTarget = this.Target != null;
			if (this._hasPulseUnsafe && this._pulseEffect != null)
			{
				global::UnityEngine.Object.Destroy(this._pulseEffect.gameObject);
				this._hasPulseUnsafe = false;
			}
			if (this.HasTarget && this.CanSeeIndicator)
			{
				this._pulseEffect = global::UnityEngine.Object.Instantiate<GameObject>(this._effectPrefab, this.Target.transform).transform;
				this._hasPulseUnsafe = true;
				global::UnityEngine.Object.Destroy(this._pulseEffect.gameObject, 3.5f);
			}
			this.Cooldown.ReadCooldown(reader);
			this.Duration.ReadCooldown(reader);
		}

		private bool CanFindTarget(out ReferenceHub bestTarget)
		{
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			float num = this._distanceThreshold * this._distanceThreshold;
			float num2 = this._dotThreshold;
			bool flag = false;
			bestTarget = null;
			Vector3 position = base.CastRole.FpcModule.Position;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					FpcStandardRoleBase fpcStandardRoleBase = referenceHub.roleManager.CurrentRole as FpcStandardRoleBase;
					if (fpcStandardRoleBase != null)
					{
						Vector3 position2 = fpcStandardRoleBase.FpcModule.Position;
						Vector3 vector = position2 - playerCameraReference.position;
						Vector3 forward = playerCameraReference.forward;
						if (Mathf.Abs((position2 - position).y) < 0.1f && vector.sqrMagnitude < 4.5f)
						{
							forward.y = 0f;
							forward.Normalize();
							vector.y = 0f;
						}
						float num3 = Vector3.Dot(forward, vector.normalized);
						if (num3 >= num2)
						{
							float sqrMagnitude = (position2 - position).sqrMagnitude;
							if (sqrMagnitude <= num)
							{
								float radius = fpcStandardRoleBase.FpcModule.CharacterControllerSettings.Radius;
								if (VisionInformation.GetVisionInformation(base.Owner, playerCameraReference, fpcStandardRoleBase.CameraPosition, radius, this._distanceThreshold, true, true, 0, false).IsLooking)
								{
									num = sqrMagnitude;
									bestTarget = referenceHub;
									num2 = num3;
									flag = true;
								}
							}
						}
					}
				}
			}
			return flag;
		}

		private const float BaseCooldown = 40f;

		private const float ReducedCooldown = 20f;

		private const float AttemptFailCooldown = 2.5f;

		private const float EffectDuration = 20f;

		private const float HeightDiffIgnoreY = 0.1f;

		private const float NearbyDistanceSqr = 4.5f;

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();

		public readonly AbilityCooldown Duration = new AbilityCooldown();

		public readonly HashSet<ReferenceHub> DeadTargets = new HashSet<ReferenceHub>();

		public readonly HashSet<ReferenceHub> SpecialZombies = new HashSet<ReferenceHub>();

		public AbilityHud SenseAbilityHUD;

		[SerializeField]
		private GameObject _effectPrefab;

		[SerializeField]
		private float _dotThreshold = 0.88f;

		[SerializeField]
		private float _distanceThreshold = 100f;

		private Scp049AttackAbility _attackAbility;

		private Transform _pulseEffect;

		private bool _hasPulseUnsafe;
	}
}
