using System;
using System.Collections.Generic;
using GameObjectPools;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Spectating;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939VisibilityController : FpcVisibilityController, IPoolResettable
	{
		public float CurrentDetectionRange
		{
			get
			{
				float defaultRange = this._defaultRange;
				return (defaultRange + defaultRange * this._focusMultiplier * this._focus.State) * Mathf.Lerp(this._exhaustionMultiplier, 1f, this._stamina.NormalizedValue);
			}
		}

		private float DetectionRangeForPlayer(ReferenceHub hub)
		{
			float num = this.CurrentDetectionRange;
			double num2;
			if (this._lastFootstepSounds.TryGetValue(hub.netId, out num2) && NetworkTime.time - num2 < (double)this._recentFootstepTime)
			{
				num *= this._recentFootstepRangeMultiplier;
			}
			if (HitboxIdentity.IsEnemy(base.Owner, hub))
			{
				FpcStandardRoleBase fpcStandardRoleBase = hub.roleManager.CurrentRole as FpcStandardRoleBase;
				if (fpcStandardRoleBase != null)
				{
					bool isJumping = fpcStandardRoleBase.FpcModule.Motor.IsJumping;
					bool flag = fpcStandardRoleBase.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting;
					double num3;
					bool flag2 = this._lastShotSound.TryGetValue(hub.netId, out num3) && NetworkTime.time - num3 < (double)this._recentFootstepTime;
					if (isJumping || flag || flag2)
					{
						return 4f;
					}
					return num;
				}
			}
			return num;
		}

		private void OnDestroy()
		{
			if (!this._wasFaded)
			{
				return;
			}
			this.ResetFade();
		}

		private void LateUpdate()
		{
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return;
			}
			if (AlphaWarheadController.Detonated)
			{
				this.ResetFade();
				return;
			}
			PlayerRolesUtils.ForEachRole<FpcStandardRoleBase>(new Action<ReferenceHub, FpcStandardRoleBase>(this.UpdateEnemies));
		}

		private void UpdateEnemies(ReferenceHub ply, FpcStandardRoleBase human)
		{
			if (!HitboxIdentity.IsEnemy(base.Owner, ply))
			{
				return;
			}
			FirstPersonMovementModule fpcModule = human.FpcModule;
			FpcMotor motor = fpcModule.Motor;
			CharacterModel characterModelInstance = fpcModule.CharacterModelInstance;
			Scp939VisibilityController.LastSeenInfo lastSeenInfo;
			bool flag = Scp939VisibilityController.LastSeen.TryGetValue(ply.netId, out lastSeenInfo);
			bool flag2 = !motor.IsInvisible;
			bool flag3 = flag2 && (Vector3.Distance(fpcModule.Position, this._scpRole.FpcModule.Position) <= this.DetectionRangeForPlayer(ply) || (flag && lastSeenInfo.Elapsed < this._sustain));
			float fade = characterModelInstance.Fade;
			characterModelInstance.Fade += Time.deltaTime * (flag3 ? this._fadeSpeed : (-this._fadeSpeed));
			this._wasFaded = true;
			if (NetworkServer.active || !base.Owner.isLocalPlayer)
			{
				return;
			}
			if (characterModelInstance.Fade == 0f)
			{
				if (fade == 0f)
				{
					return;
				}
				characterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity x)
				{
					x.SetColliders(false);
				});
				return;
			}
			else
			{
				if (!flag2)
				{
					fpcModule.Position = (flag ? lastSeenInfo.WorldPos : motor.ReceivedPosition.Position);
					return;
				}
				if (flag3 && fade == 0f)
				{
					fpcModule.Position = motor.ReceivedPosition.Position;
					AnimatedCharacterModel animatedCharacterModel = characterModelInstance as AnimatedCharacterModel;
					if (animatedCharacterModel != null)
					{
						animatedCharacterModel.ForceUpdate();
					}
					HitboxIdentity[] hitboxes = characterModelInstance.Hitboxes;
					for (int i = 0; i < hitboxes.Length; i++)
					{
						hitboxes[i].SetColliders(true);
					}
				}
				return;
			}
		}

		private void ResetFade()
		{
			PlayerRolesUtils.ForEachRole<FpcStandardRoleBase>(delegate(FpcStandardRoleBase x)
			{
				CharacterModel characterModelInstance = x.FpcModule.CharacterModelInstance;
				if (characterModelInstance.Fade == 1f)
				{
					return;
				}
				characterModelInstance.Fade = 1f;
				characterModelInstance.Hitboxes.ForEach(delegate(HitboxIdentity hitbox)
				{
					hitbox.SetColliders(true);
				});
			});
		}

		private void OnFootstepPlayed(AnimatedCharacterModel model, float range)
		{
			ReferenceHub ownerHub = model.OwnerHub;
			if (!HitboxIdentity.IsEnemy(base.Owner, ownerHub))
			{
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = ownerHub.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return;
			}
			if ((fpcStandardRoleBase.FpcModule.Position - this._scpRole.FpcModule.Position).sqrMagnitude > range * range)
			{
				return;
			}
			this._lastFootstepSounds[ownerHub.netId] = NetworkTime.time;
		}

		private void OnSpectatorTargetChanged()
		{
			if (!this._wasFaded)
			{
				return;
			}
			this.ResetFade();
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			if (!this._wasFaded || !hub.isLocalPlayer || newRole is SpectatorRole)
			{
				return;
			}
			this.ResetFade();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._scpRole = base.Role as Scp939Role;
			base.Owner.playerStats.TryGetModule<StaminaStat>(out this._stamina);
			this._scpRole.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focus);
			SpectatorTargetTracker.OnTargetChanged += this.OnSpectatorTargetChanged;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Combine(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(this.OnFootstepPlayed));
			if (!base.Owner.isLocalPlayer)
			{
				return;
			}
			Scp939VisibilityController.LastSeen.Clear();
		}

		public override bool ValidateVisibility(ReferenceHub hub)
		{
			if (!base.ValidateVisibility(hub))
			{
				return false;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, hub))
			{
				return true;
			}
			FpcStandardRoleBase fpcStandardRoleBase = hub.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return true;
			}
			if (AlphaWarheadController.Detonated)
			{
				return true;
			}
			FirstPersonMovementModule fpcModule = this._scpRole.FpcModule;
			float num = this.BaseRangeForPlayer(hub, fpcStandardRoleBase);
			num = Mathf.Max(num, this.DetectionRangeForPlayer(hub));
			num += fpcModule.MaxMovementSpeed * this._pingTolerance;
			bool flag = (fpcStandardRoleBase.FpcModule.Position - fpcModule.Position).sqrMagnitude <= num * num;
			Scp939VisibilityController.LastSeenInfo lastSeenInfo;
			bool flag2 = flag || (Scp939VisibilityController.LastSeen.TryGetValue(hub.netId, out lastSeenInfo) && lastSeenInfo.Elapsed < this._sustain);
			if (!flag || this._scpRole.IsLocalPlayer)
			{
				return flag2;
			}
			Scp939VisibilityController.LastSeen[hub.netId] = new Scp939VisibilityController.LastSeenInfo
			{
				RelPos = new RelativePosition(fpcModule.Position),
				Time = NetworkTime.time,
				Velocity = fpcModule.Motor.Velocity
			};
			return true;
		}

		private float BaseRangeForPlayer(ReferenceHub hub, FpcStandardRoleBase targetRole)
		{
			float num = 0f;
			ItemBase curInstance = hub.inventory.CurInstance;
			ISoundEmittingItem soundEmittingItem = curInstance as ISoundEmittingItem;
			float num2;
			if (soundEmittingItem != null && curInstance != null && soundEmittingItem.ServerTryGetSoundEmissionRange(out num2))
			{
				num = num2;
			}
			HumanVoiceModule humanVoiceModule = targetRole.VoiceModule as HumanVoiceModule;
			if (humanVoiceModule != null && humanVoiceModule.ServerIsSending)
			{
				num = Mathf.Max(num, humanVoiceModule.ProximityPlayback.Source.maxDistance);
			}
			return num;
		}

		public void ResetObject()
		{
			SpectatorTargetTracker.OnTargetChanged -= this.OnSpectatorTargetChanged;
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Remove(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(this.OnFootstepPlayed));
			if (!this._wasFaded)
			{
				return;
			}
			this.ResetFade();
		}

		private const float DetectionRangeForShootingCrouchingOrJumping = 4f;

		[SerializeField]
		private float _pingTolerance;

		[SerializeField]
		private float _defaultRange;

		[SerializeField]
		private float _recentFootstepRangeMultiplier;

		[SerializeField]
		private float _recentFootstepTime;

		[SerializeField]
		private float _focusMultiplier;

		[SerializeField]
		private float _exhaustionMultiplier;

		[SerializeField]
		private float _fadeSpeed;

		[SerializeField]
		private float _sustain;

		private Scp939Role _scpRole;

		private StaminaStat _stamina;

		private Scp939FocusAbility _focus;

		private bool _wasFaded;

		private static readonly Dictionary<uint, Scp939VisibilityController.LastSeenInfo> LastSeen = new Dictionary<uint, Scp939VisibilityController.LastSeenInfo>();

		private readonly Dictionary<uint, double> _lastFootstepSounds = new Dictionary<uint, double>();

		private readonly Dictionary<uint, double> _lastShotSound = new Dictionary<uint, double>();

		private struct LastSeenInfo
		{
			public Vector3 WorldPos
			{
				get
				{
					return this.RelPos.Position + this.Velocity * this.Elapsed;
				}
			}

			public float Elapsed
			{
				get
				{
					return (float)(NetworkTime.time - this.Time);
				}
			}

			public double Time;

			public RelativePosition RelPos;

			public Vector3 Velocity;
		}
	}
}
