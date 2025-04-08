using System;
using System.Collections.Generic;
using GameObjectPools;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096AttackAbility : KeySubroutine<Scp096Role>, IPoolResettable
	{
		private bool AttackPossible
		{
			get
			{
				return base.CastRole.IsRageState(Scp096RageState.Enraged) && base.CastRole.IsAbilityState(Scp096AbilityState.None);
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		public bool LeftAttack { get; private set; }

		public event Action<Scp096HitResult> OnHitReceived;

		public event Action OnAttackTriggered;

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			byte b = (byte)this._hitResult;
			if (this.LeftAttack)
			{
				b |= 64;
			}
			writer.WriteByte(b);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			byte b = reader.ReadByte();
			this.LeftAttack = (b & 64) > 0;
			Scp096HitResult scp096HitResult = (Scp096HitResult)((int)b & -65);
			Action<Scp096HitResult> onHitReceived = this.OnHitReceived;
			if (onHitReceived != null)
			{
				onHitReceived(scp096HitResult);
			}
			if (scp096HitResult == Scp096HitResult.None)
			{
				return;
			}
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return;
			}
			Hitmarker.PlayHitmarker(1f, true);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
			writer.WriteQuaternion(base.Owner.PlayerCameraReference.rotation);
			foreach (ReferenceHub referenceHub in Scp096AttackAbility.PlayersToSend)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					Vector3 position = fpcRole.FpcModule.Position;
					writer.WriteReferenceHub(referenceHub);
					writer.WriteRelativePosition(new RelativePosition(position));
				}
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this._serverAttackCooldown.TolerantIsReady || !this.AttackPossible)
			{
				return;
			}
			Scp096AttackAbility.BacktrackedPlayers.Clear();
			RelativePosition relativePosition = reader.ReadRelativePosition();
			Scp096AttackAbility.BacktrackedPlayers.Add(new FpcBacktracker(base.Owner, relativePosition.Position, reader.ReadQuaternion(), 0.1f, 0.15f));
			while (reader.Position < reader.Capacity)
			{
				ReferenceHub referenceHub;
				bool flag = reader.TryReadReferenceHub(out referenceHub);
				Vector3 position = reader.ReadRelativePosition().Position;
				if (flag)
				{
					Scp096AttackAbility.BacktrackedPlayers.Add(new FpcBacktracker(referenceHub, position, 0.4f));
				}
			}
			this.ServerAttack();
			Scp096AttackAbility.BacktrackedPlayers.ForEach(delegate(FpcBacktracker x)
			{
				x.RestorePosition();
			});
			this._serverAttackCooldown.Trigger(0.5);
		}

		private void ServerAttack()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.LeftAttack = !this.LeftAttack;
			base.CastRole.StateController.SetAbilityState(Scp096AbilityState.Attacking);
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			Scp096HitHandler scp096HitHandler = (this.LeftAttack ? this._leftHitHandler : this._rightHitHandler);
			scp096HitHandler.Clear();
			this._hitResult = scp096HitHandler.DamageSphere(playerCameraReference.position + playerCameraReference.forward * this._sphereHitboxOffset, this._sphereHitboxRadius);
			this._audioPlayer.ServerPlayAttack(this._hitResult);
			base.ServerSendRpc(true);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._clientAttackCooldown.Clear();
			this._serverAttackCooldown.Clear();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._leftHitHandler = new Scp096HitHandler(base.CastRole, Scp096DamageHandler.AttackType.SlapLeft, 500f, 250f, 60f, 0f);
			this._rightHitHandler = new Scp096HitHandler(base.CastRole, Scp096DamageHandler.AttackType.SlapRight, 500f, 250f, 60f, 0f);
		}

		protected override void Update()
		{
			base.Update();
			if (!NetworkServer.active)
			{
				return;
			}
			if (!this._serverAttackCooldown.IsReady)
			{
				return;
			}
			if (!base.CastRole.IsAbilityState(Scp096AbilityState.Attacking))
			{
				return;
			}
			base.CastRole.ResetAbilityState();
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this.AttackPossible)
			{
				return;
			}
			if (!this._clientAttackCooldown.IsReady)
			{
				return;
			}
			Scp096AttackAbility.PlayersToSend.Clear();
			Vector3 position = base.CastRole.FpcModule.Position;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null && HitboxIdentity.IsEnemy(base.Owner, referenceHub) && (fpcRole.FpcModule.Position - position).sqrMagnitude <= 9f)
				{
					Scp096AttackAbility.PlayersToSend.Add(referenceHub);
				}
			}
			base.ClientSendCmd();
			this._clientAttackCooldown.Trigger(0.5);
			Action onAttackTriggered = this.OnAttackTriggered;
			if (onAttackTriggered == null)
			{
				return;
			}
			onAttackTriggered();
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp096AudioPlayer>(out this._audioPlayer);
		}

		public const float DefaultAttackCooldown = 0.5f;

		private const float HumanDamage = 60f;

		private const float DoorDamage = 250f;

		private const int WindowDamage = 500;

		private const float BacktrackingDisSqr = 9f;

		private const byte LeftAttackSyncCode = 64;

		[SerializeField]
		private float _sphereHitboxRadius;

		[SerializeField]
		private float _sphereHitboxOffset;

		private static readonly List<FpcBacktracker> BacktrackedPlayers = new List<FpcBacktracker>();

		private static readonly List<ReferenceHub> PlayersToSend = new List<ReferenceHub>();

		private readonly AbilityCooldown _clientAttackCooldown = new AbilityCooldown();

		private readonly TolerantAbilityCooldown _serverAttackCooldown = new TolerantAbilityCooldown(0.2f);

		private Scp096HitHandler _leftHitHandler;

		private Scp096HitHandler _rightHitHandler;

		private Scp096AudioPlayer _audioPlayer;

		private Scp096HitResult _hitResult;
	}
}
