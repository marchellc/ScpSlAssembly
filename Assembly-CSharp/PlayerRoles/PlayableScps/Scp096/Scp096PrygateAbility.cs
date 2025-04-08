using System;
using Interactables.Interobjects;
using Mirror;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096PrygateAbility : StandardSubroutine<Scp096Role>
	{
		public void ClientTryPry(PryableDoor door)
		{
			this._syncDoor = door;
			base.ClientSendCmd();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._hitHandler = new Scp096HitHandler(base.CastRole, Scp096DamageHandler.AttackType.GateKill, 0f, 0f, 200f, 200f);
		}

		public override void ResetObject()
		{
			base.ResetObject();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteNetworkBehaviour(this._syncDoor);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (base.CastRole.StateController.RageState != Scp096RageState.Enraged)
			{
				return;
			}
			if (base.CastRole.StateController.AbilityState == Scp096AbilityState.PryingGate)
			{
				return;
			}
			this._syncDoor = reader.ReadNetworkBehaviour<PryableDoor>();
			if (this._syncDoor == null)
			{
				return;
			}
			if (this._syncDoor.TargetState || this._syncDoor.GetExactState() > 0f)
			{
				return;
			}
			if ((this._syncDoor.transform.position - base.CastRole.FpcModule.Position).sqrMagnitude > 8.12f)
			{
				return;
			}
			ReferenceHub referenceHub;
			base.Role.TryGetOwner(out referenceHub);
			if (!this._syncDoor.TryPryGate(referenceHub))
			{
				return;
			}
			this._hitHandler.Clear();
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteNetworkBehaviour(this._syncDoor);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._syncDoor = reader.ReadNetworkBehaviour<PryableDoor>();
			if (!NetworkServer.active && !base.Owner.isLocalPlayer)
			{
				return;
			}
			(base.CastRole.FpcModule as Scp096MovementModule).SetTargetGate(this._syncDoor);
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp096AudioPlayer>(out this._audioPlayer);
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (!base.CastRole.IsAbilityState(Scp096AbilityState.PryingGate))
			{
				return;
			}
			if (this._syncDoor == null)
			{
				return;
			}
			Vector3 vector = this._syncDoor.transform.position + Vector3.up * 1.5f;
			Scp096HitResult scp096HitResult = this._hitHandler.DamageSphere(vector, 0.2f);
			if (scp096HitResult == Scp096HitResult.None)
			{
				return;
			}
			this._audioPlayer.ServerPlayAttack(scp096HitResult);
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, true);
		}

		private PryableDoor _syncDoor;

		private Scp096HitHandler _hitHandler;

		private Scp096AudioPlayer _audioPlayer;

		private const float DoorKillerHeight = 1.5f;

		private const float DoorKillerRadius = 0.2f;

		private const float MaxDisSqr = 8.12f;

		private const float HumanDamage = 200f;
	}
}
