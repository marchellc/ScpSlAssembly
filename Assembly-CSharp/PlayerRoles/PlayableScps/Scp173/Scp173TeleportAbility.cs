using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173TeleportAbility : KeySubroutine<Scp173Role>
	{
		private float EffectiveBlinkDistance
		{
			get
			{
				return 8f * (this._breakneckSpeedsAbility.IsActive ? 1.8f : 1f);
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Zoom;
			}
		}

		public ReferenceHub BestTarget
		{
			get
			{
				ReferenceHub referenceHub = null;
				float num = float.MaxValue;
				foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
				{
					if (HitboxIdentity.IsEnemy(base.Owner, referenceHub2))
					{
						IFpcRole fpcRole = referenceHub2.roleManager.CurrentRole as IFpcRole;
						if (fpcRole != null)
						{
							Vector3 position = fpcRole.FpcModule.Position;
							Vector3 tpPosition = this._tpPosition;
							if ((position - tpPosition).MagnitudeOnlyY() < 2.2f)
							{
								position.y = 0f;
								tpPosition.y = 0f;
							}
							float sqrMagnitude = (position - tpPosition).sqrMagnitude;
							if (sqrMagnitude <= num)
							{
								if (Physics.Linecast(tpPosition, position, PlayerRolesUtils.BlockerMask))
								{
									num = Mathf.Min(sqrMagnitude, 1.66f);
								}
								else
								{
									num = sqrMagnitude;
									referenceHub = referenceHub2;
								}
							}
						}
					}
				}
				if (num <= 1.66f)
				{
					return referenceHub;
				}
				return null;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this._fpcModule = base.CastRole.FpcModule as Scp173MovementModule;
			SubroutineManagerModule subroutineModule = base.CastRole.SubroutineModule;
			subroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
			subroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
			subroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out this._audioSubroutine);
			subroutineModule.TryGetSubroutine<Scp173BlinkTimer>(out this._blinkTimer);
		}

		protected override void Update()
		{
			base.Update();
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				if (this._isAiming)
				{
					this._isAiming = false;
					this._tpIndicator.UpdateVisibility(false);
				}
				return;
			}
			bool flag = (base.Owner.isLocalPlayer ? (this.IsKeyHeld && !Cursor.visible) : this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.Aiming));
			if (this._isAiming)
			{
				this.UpdateAiming(!flag);
				return;
			}
			if (flag)
			{
				this._isAiming = true;
			}
		}

		private void UpdateAiming(bool wantsToTeleport)
		{
			bool flag = this._fpcModule.TryGetTeleportPos(this.EffectiveBlinkDistance, out this._tpPosition, out this._targetDis);
			if (!wantsToTeleport)
			{
				this._tpIndicator.UpdateVisibility(flag && this._blinkTimer.AbilityReady);
				this._tpIndicator.transform.position = this._tpPosition;
				if (!this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.Aiming))
				{
					this._cmdData = Scp173TeleportAbility.CmdTeleportData.Aiming;
					base.ClientSendCmd();
				}
				return;
			}
			if (base.Owner.isLocalPlayer)
			{
				this._cmdData = (flag ? Scp173TeleportAbility.CmdTeleportData.WantsToTeleport : Scp173TeleportAbility.CmdTeleportData.Aiming);
				base.ClientSendCmd();
			}
			this._isAiming = false;
			this._tpIndicator.UpdateVisibility(false);
		}

		private bool TryBlink(float maxDis)
		{
			maxDis = Mathf.Clamp(maxDis, 0f, this.EffectiveBlinkDistance);
			if (!this._blinkTimer.AbilityReady)
			{
				return false;
			}
			float num;
			if (!this._fpcModule.TryGetTeleportPos(maxDis, out this._tpPosition, out num))
			{
				return false;
			}
			float num2 = this._fpcModule.CharController.height / 2f;
			this._blinkTimer.ServerBlink(this._tpPosition + Vector3.up * num2);
			return true;
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteByte((byte)this._cmdData);
			if (!this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.WantsToTeleport))
			{
				return;
			}
			writer.WriteQuaternion(base.Owner.PlayerCameraReference.rotation);
			writer.WriteFloat(this._targetDis + 0.1f);
			writer.WriteReferenceHub(this.BestTarget);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._cmdData = (Scp173TeleportAbility.CmdTeleportData)reader.ReadByte();
			if (!this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.WantsToTeleport))
			{
				base.ServerSendRpc(true);
				return;
			}
			if (!this._blinkTimer.AbilityReady)
			{
				return;
			}
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			HashSet<ReferenceHub> prevObservers = new HashSet<ReferenceHub>(this._observersTracker.Observers);
			Scp173TeleportAbility.CmdTeleportData cmdData = this._cmdData;
			this._cmdData = (Scp173TeleportAbility.CmdTeleportData)0;
			base.ServerSendRpc(true);
			this._cmdData = cmdData;
			Quaternion rotation = playerCameraReference.rotation;
			playerCameraReference.rotation = reader.ReadQuaternion();
			bool flag = this.TryBlink(reader.ReadFloat());
			playerCameraReference.rotation = rotation;
			if (!flag)
			{
				return;
			}
			prevObservers.UnionWith(this._observersTracker.Observers);
			base.ServerSendRpc((ReferenceHub x) => prevObservers.Contains(x));
			this._audioSubroutine.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Teleport);
			if (this._breakneckSpeedsAbility.IsActive)
			{
				return;
			}
			int num = Physics.OverlapSphereNonAlloc(this._fpcModule.Position, 0.8f, Scp173TeleportAbility.DetectedColliders, 16384);
			for (int i = 0; i < num; i++)
			{
				BreakableWindow breakableWindow;
				if (Scp173TeleportAbility.DetectedColliders[i].TryGetComponent<BreakableWindow>(out breakableWindow))
				{
					breakableWindow.Damage(breakableWindow.health, base.CastRole.DamageHandler, Vector3.zero);
				}
			}
			ReferenceHub referenceHub = reader.ReadReferenceHub();
			if (referenceHub == null || !HitboxIdentity.IsEnemy(base.Owner, referenceHub))
			{
				return;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			Bounds bounds = fpcRole.FpcModule.Tracer.GenerateBounds(0.4f, true);
			Vector3 position = fpcRole.FpcModule.Position;
			bounds.Encapsulate(new Bounds(position, Vector3.up * 2.2f));
			if (bounds.SqrDistance(this._fpcModule.Position) > 1.66f)
			{
				return;
			}
			if (Physics.Linecast(this._tpPosition, position, PlayerRolesUtils.BlockerMask))
			{
				return;
			}
			if (!referenceHub.playerStats.DealDamage(base.CastRole.DamageHandler))
			{
				return;
			}
			Scp173AudioPlayer scp173AudioPlayer;
			if (!base.CastRole.SubroutineModule.TryGetSubroutine<Scp173AudioPlayer>(out scp173AudioPlayer))
			{
				return;
			}
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
			scp173AudioPlayer.ServerSendSound(Scp173AudioPlayer.Scp173SoundId.Snap);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._cmdData);
			if (!this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.WantsToTeleport))
			{
				return;
			}
			writer.WriteRelativePosition(new RelativePosition(this._fpcModule.Position));
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._cmdData = (Scp173TeleportAbility.CmdTeleportData)reader.ReadByte();
			if (!this.HasDataFlag(Scp173TeleportAbility.CmdTeleportData.WantsToTeleport))
			{
				return;
			}
			RelativePosition relativePosition = reader.ReadRelativePosition();
			this._fpcModule.Motor.ReceivedPosition = relativePosition;
			this._fpcModule.Position = relativePosition.Position;
			this._lastBlink = Time.timeSinceLevelLoad;
			this._blinkEffect.weight = 1f;
			(this._fpcModule.CharacterModelInstance as Scp173CharacterModel).Frozen = false;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._lastBlink = 0f;
		}

		private bool HasDataFlag(Scp173TeleportAbility.CmdTeleportData ctd)
		{
			return (this._cmdData & ctd) == ctd;
		}

		private const float BlinkDistance = 8f;

		private const float BreakneckDistanceMultiplier = 1.8f;

		private const float KillRadiusSqr = 1.66f;

		private const float KillHeight = 2.2f;

		private const float KillBacktracking = 0.4f;

		private const float ClientDistanceAddition = 0.1f;

		private const int GlassLayerMask = 16384;

		private const float GlassDestroyRadius = 0.8f;

		private static readonly Collider[] DetectedColliders = new Collider[8];

		private Scp173MovementModule _fpcModule;

		private Scp173ObserversTracker _observersTracker;

		private Scp173BreakneckSpeedsAbility _breakneckSpeedsAbility;

		private Scp173BlinkTimer _blinkTimer;

		private Scp173AudioPlayer _audioSubroutine;

		private bool _isAiming;

		private float _targetDis;

		private Vector3 _tpPosition;

		private float _lastBlink;

		private Scp173TeleportAbility.CmdTeleportData _cmdData;

		[SerializeField]
		private Scp173TeleportIndicator _tpIndicator;

		[SerializeField]
		private AnimationCurve _blinkIntensity;

		[SerializeField]
		private Volume _blinkEffect;

		[Flags]
		private enum CmdTeleportData
		{
			Aiming = 1,
			WantsToTeleport = 2
		}
	}
}
