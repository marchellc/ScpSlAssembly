using System;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class SnowballProjectile : FlybyDetectorProjectile
{
	public override void ServerProcessHit(HitboxIdentity hid)
	{
		base.ServerProcessHit(hid);
		float num = 15f;
		bool flag = false;
		bool flag2 = false;
		if (hid.HitboxType == HitboxType.Headshot)
		{
			flag = true;
			num *= 2f;
		}
		if (SnowballItem.IsCooked(this.Info.Serial, true))
		{
			flag2 = true;
			num *= 2f;
		}
		if (flag && flag2)
		{
			this.RpcBonk();
		}
		ReferenceHub targetHub = hid.TargetHub;
		targetHub.playerEffectsController.EnableEffect<Snowed>(5f, false);
		SnowballDamageHandler snowballDamageHandler = new SnowballDamageHandler(this.PreviousOwner, num, base.TrajPhysics.LastVelocity);
		targetHub.playerStats.DealDamage(snowballDamageHandler);
		Hitmarker.SendHitmarkerConditionally(num / 15f, snowballDamageHandler, targetHub);
	}

	[ClientRpc]
	private void RpcBonk()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void SnowballProjectile::RpcBonk()", 673396225, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcBonk()
	{
		this._bonk.Play();
	}

	protected static void InvokeUserCode_RpcBonk(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcBonk called on server.");
			return;
		}
		((SnowballProjectile)obj).UserCode_RpcBonk();
	}

	static SnowballProjectile()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(SnowballProjectile), "System.Void SnowballProjectile::RpcBonk()", new RemoteCallDelegate(SnowballProjectile.InvokeUserCode_RpcBonk));
	}

	[SerializeField]
	private AudioSource _bonk;

	private const float BaseDamage = 15f;

	private const float SnowedDuration = 5f;
}
