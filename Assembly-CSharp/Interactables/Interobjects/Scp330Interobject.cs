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

namespace Interactables.Interobjects;

public class Scp330Interobject : NetworkBehaviour, IServerInteractable, IInteractable
{
	private readonly List<Footprint> _previousUses = new List<Footprint>();

	[SerializeField]
	private AudioClip _takeSound;

	private const float TakeCooldown = 0.1f;

	private const int MaxAmountPerLife = 2;

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!ply.IsHuman() || ply.HasBlock(BlockedInteraction.GrabItems))
		{
			return;
		}
		float num = 0.1f;
		int num2 = 0;
		bool playSound = true;
		bool allowPunishment = true;
		foreach (Footprint previousUse in this._previousUses)
		{
			if (previousUse.LifeIdentifier == ply.roleManager.CurrentRole.UniqueLifeIdentifier)
			{
				double totalSeconds = previousUse.Stopwatch.Elapsed.TotalSeconds;
				num = Mathf.Min(num, (float)totalSeconds);
				num2++;
			}
		}
		if (num < 0.1f || !Scp330Bag.CanAddCandy(ply))
		{
			return;
		}
		CandyKindID random = Scp330Candies.GetRandom();
		PlayerInteractingScp330EventArgs e = new PlayerInteractingScp330EventArgs(ply, num2, playSound, allowPunishment, random);
		PlayerEvents.OnInteractingScp330(e);
		if (!e.IsAllowed)
		{
			return;
		}
		playSound = e.PlaySound;
		allowPunishment = e.AllowPunishment;
		num2 = e.Uses;
		random = e.CandyType;
		if (random != CandyKindID.None && Scp330Bag.TryAddCandy(ply, random))
		{
			if (playSound)
			{
				this.RpcMakeSound();
			}
			if (allowPunishment && num2 >= 2)
			{
				ply.playerEffectsController.EnableEffect<SeveredHands>();
				this.ClearUsesForRole(ply.roleManager.CurrentRole);
			}
			else
			{
				this._previousUses.Add(new Footprint(ply));
			}
			PlayerEvents.OnInteractedScp330(new PlayerInteractedScp330EventArgs(ply, num2, playSound, allowPunishment, random));
		}
	}

	private void ClearUsesForRole(PlayerRoleBase prb)
	{
		for (int num = this._previousUses.Count - 1; num >= 0; num--)
		{
			if (this._previousUses[num].LifeIdentifier == prb.UniqueLifeIdentifier)
			{
				this._previousUses.RemoveAt(num);
			}
		}
	}

	[ClientRpc]
	private void RpcMakeSound()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Interactables.Interobjects.Scp330Interobject::RpcMakeSound()", 1231574529, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcMakeSound()
	{
		AudioSourcePoolManager.PlayOnTransform(this._takeSound, base.transform);
	}

	protected static void InvokeUserCode_RpcMakeSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMakeSound called on server.");
		}
		else
		{
			((Scp330Interobject)obj).UserCode_RpcMakeSound();
		}
	}

	static Scp330Interobject()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Scp330Interobject), "System.Void Interactables.Interobjects.Scp330Interobject::RpcMakeSound()", InvokeUserCode_RpcMakeSound);
	}
}
