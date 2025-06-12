using System.Diagnostics;
using GameObjectPools;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp079.Cameras;

public class Scp079CameraRotationSync : SubroutineBase, IPoolSpawnable
{
	private Scp079Role _role;

	private Scp079CurrentCameraSync _curCamSync;

	private Scp079LostSignalHandler _lostSignalHandler;

	private ReferenceHub _owner;

	private readonly Stopwatch _clientSendLimit = Stopwatch.StartNew();

	private const float ClientSendRate = 15f;

	private void Update()
	{
		if (this._owner.isLocalPlayer && !(this._clientSendLimit.Elapsed.TotalSeconds < 0.06666667014360428))
		{
			base.ClientSendCmd();
			this._clientSendLimit.Restart();
		}
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		if (this._curCamSync.TryGetCurrentCamera(out var cam))
		{
			writer.WriteUShort(cam.SyncId);
			cam.WriteAxes(writer);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this._curCamSync.TryGetCurrentCamera(out var cam) && cam.SyncId == reader.ReadUShort() && !this._lostSignalHandler.Lost)
		{
			cam.ApplyAxes(reader);
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		if (this._curCamSync.TryGetCurrentCamera(out var cam))
		{
			writer.WriteUShort(cam.SyncId);
			cam.WriteAxes(writer);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (Scp079InteractableBase.TryGetInteractable(reader.ReadUShort(), out Scp079Camera result))
		{
			result.ApplyAxes(reader);
		}
	}

	public void SpawnObject()
	{
		this._role = base.Role as Scp079Role;
		this._role.TryGetOwner(out this._owner);
		this._role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		this._role.SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._lostSignalHandler);
	}
}
