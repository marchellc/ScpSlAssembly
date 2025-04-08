using System;
using System.Collections.Generic;
using AudioPooling;
using CustomPlayerEffects;
using Footprinting;
using Interactables.Verification;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class Scp330Interobject : NetworkBehaviour, IServerInteractable, IInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (!ply.IsHuman() || ply.HasBlock(BlockedInteraction.GrabItems))
			{
				return;
			}
			float num = 0.1f;
			int num2 = 0;
			bool flag = true;
			bool flag2 = true;
			foreach (Footprint footprint in this._previousUses)
			{
				if (footprint.LifeIdentifier == ply.roleManager.CurrentRole.UniqueLifeIdentifier)
				{
					double totalSeconds = footprint.Stopwatch.Elapsed.TotalSeconds;
					num = Mathf.Min(num, (float)totalSeconds);
					num2++;
				}
			}
			if (num < 0.1f)
			{
				return;
			}
			if (!Scp330Bag.CanAddCandy(ply))
			{
				return;
			}
			CandyKindID candyKindID = Scp330Candies.GetRandom(CandyKindID.None);
			PlayerInteractingScp330EventArgs playerInteractingScp330EventArgs = new PlayerInteractingScp330EventArgs(ply, num2, flag, flag2, candyKindID);
			PlayerEvents.OnInteractingScp330(playerInteractingScp330EventArgs);
			if (!playerInteractingScp330EventArgs.IsAllowed)
			{
				return;
			}
			flag = playerInteractingScp330EventArgs.PlaySound;
			flag2 = playerInteractingScp330EventArgs.AllowPunishment;
			num2 = playerInteractingScp330EventArgs.Uses;
			candyKindID = playerInteractingScp330EventArgs.CandyType;
			if (candyKindID == CandyKindID.None)
			{
				return;
			}
			if (!Scp330Bag.TryAddCandy(ply, candyKindID))
			{
				return;
			}
			if (flag)
			{
				this.RpcMakeSound();
			}
			if (flag2 && num2 >= 2)
			{
				ply.playerEffectsController.EnableEffect<SeveredHands>(0f, false);
				this.ClearUsesForRole(ply.roleManager.CurrentRole);
			}
			else
			{
				this._previousUses.Add(new Footprint(ply));
			}
			PlayerEvents.OnInteractedScp330(new PlayerInteractedScp330EventArgs(ply, num2, flag, flag2, candyKindID));
		}

		private void ClearUsesForRole(PlayerRoleBase prb)
		{
			for (int i = this._previousUses.Count - 1; i >= 0; i--)
			{
				if (this._previousUses[i].LifeIdentifier == prb.UniqueLifeIdentifier)
				{
					this._previousUses.RemoveAt(i);
				}
			}
		}

		[ClientRpc]
		private void RpcMakeSound()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Interactables.Interobjects.Scp330Interobject::RpcMakeSound()", 1231574529, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcMakeSound()
		{
			AudioSourcePoolManager.PlayOnTransform(this._takeSound, base.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		protected static void InvokeUserCode_RpcMakeSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcMakeSound called on server.");
				return;
			}
			((Scp330Interobject)obj).UserCode_RpcMakeSound();
		}

		static Scp330Interobject()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp330Interobject), "System.Void Interactables.Interobjects.Scp330Interobject::RpcMakeSound()", new RemoteCallDelegate(Scp330Interobject.InvokeUserCode_RpcMakeSound));
		}

		private readonly List<Footprint> _previousUses = new List<Footprint>();

		[SerializeField]
		private AudioClip _takeSound;

		private const float TakeCooldown = 0.1f;

		private const int MaxAmountPerLife = 2;
	}
}
