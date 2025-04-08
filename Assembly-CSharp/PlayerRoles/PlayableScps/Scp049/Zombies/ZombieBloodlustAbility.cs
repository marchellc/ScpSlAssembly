using System;
using System.Diagnostics;
using CustomPlayerEffects;
using GameObjectPools;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieBloodlustAbility : SubroutineBase, IPoolResettable
	{
		public bool LookingAtTarget { get; private set; }

		public float SimulatedStare
		{
			get
			{
				return Mathf.Max(0f, this._simulatedStareTime - (float)this._simulatedStareSw.Elapsed.TotalSeconds);
			}
			set
			{
				this._simulatedStareTime = value;
				this._simulatedStareSw.Restart();
			}
		}

		private void Update()
		{
			this.RefreshChaseState();
		}

		public void RefreshChaseState()
		{
			ReferenceHub referenceHub;
			if (!NetworkServer.active || !base.Role.TryGetOwner(out referenceHub))
			{
				return;
			}
			this.LookingAtTarget = this.SimulatedStare > 0f || this.AnyTargets(referenceHub, referenceHub.PlayerCameraReference);
			base.ServerSendRpc(true);
		}

		private bool AnyTargets(ReferenceHub owner, Transform camera)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.IsHuman() && !referenceHub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null && VisionInformation.GetVisionInformation(owner, camera, fpcRole.FpcModule.Position, fpcRole.FpcModule.CharacterControllerSettings.Radius, this._maxViewDistance, true, true, 0, false).IsLooking)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBool(this.LookingAtTarget);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.LookingAtTarget = reader.ReadBool();
		}

		public void ResetObject()
		{
			this._simulatedStareTime = 0f;
		}

		[SerializeField]
		private float _maxViewDistance;

		private float _simulatedStareTime;

		private readonly Stopwatch _simulatedStareSw = Stopwatch.StartNew();
	}
}
