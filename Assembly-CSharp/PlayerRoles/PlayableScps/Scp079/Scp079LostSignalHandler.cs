using System;
using GameObjectPools;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079LostSignalHandler : SubroutineBase, IPoolSpawnable
{
	[SerializeField]
	private float _ghostlightLockoutDuration;

	private Scp079CurrentCameraSync _curCamSync;

	private Scp079AuxManager _auxManager;

	private double _recoveryTime;

	private bool _prevLost;

	public bool Lost => this._recoveryTime > NetworkTime.time;

	public float RemainingTime => Mathf.Max(0f, (float)(this._recoveryTime - NetworkTime.time));

	public event Action OnStatusChanged;

	private void Update()
	{
		bool lost = this.Lost;
		if (NetworkServer.active && lost)
		{
			this._auxManager.CurrentAux = 0f;
		}
		if (this._prevLost != lost)
		{
			this._prevLost = lost;
			this.OnStatusChanged?.Invoke();
			if (this._curCamSync.TryGetCurrentCamera(out var cam))
			{
				cam.IsActive = !lost;
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		SubroutineManagerModule subroutineModule = (base.Role as ISubroutinedRole).SubroutineModule;
		subroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		subroutineModule.TryGetSubroutine<Scp079AuxManager>(out this._auxManager);
		Scp2176Projectile.OnServerShattered += delegate(Scp2176Projectile projectile, RoomIdentifier rid)
		{
			if (NetworkServer.active && !base.Role.Pooled && !(rid != this._curCamSync.CurrentCamera.Room) && base.Role.TryGetOwner(out var hub) && !hub.characterClassManager.GodMode)
			{
				this.ServerLoseSignal(this.Lost ? 0f : this._ghostlightLockoutDuration);
			}
		};
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteDouble(this._recoveryTime);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._recoveryTime = reader.ReadDouble();
	}

	public void ServerLoseSignal(float duration)
	{
		this._recoveryTime = NetworkTime.time + (double)duration;
		base.ServerSendRpc(toAll: true);
	}

	public void SpawnObject()
	{
		this._recoveryTime = 0.0;
		this._prevLost = false;
	}
}
