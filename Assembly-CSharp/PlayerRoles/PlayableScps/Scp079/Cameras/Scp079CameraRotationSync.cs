using System;
using System.Diagnostics;
using GameObjectPools;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp079.Cameras
{
	public class Scp079CameraRotationSync : SubroutineBase, IPoolSpawnable
	{
		private void Update()
		{
			if (!this._owner.isLocalPlayer)
			{
				return;
			}
			if (this._clientSendLimit.Elapsed.TotalSeconds < 0.06666667014360428)
			{
				return;
			}
			base.ClientSendCmd();
			this._clientSendLimit.Restart();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			Scp079Camera scp079Camera;
			if (!this._curCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			writer.WriteUShort(scp079Camera.SyncId);
			scp079Camera.WriteAxes(writer);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			Scp079Camera scp079Camera;
			if (!this._curCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			if (scp079Camera.SyncId != reader.ReadUShort() || this._lostSignalHandler.Lost)
			{
				return;
			}
			scp079Camera.ApplyAxes(reader);
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			Scp079Camera scp079Camera;
			if (!this._curCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			writer.WriteUShort(scp079Camera.SyncId);
			scp079Camera.WriteAxes(writer);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			Scp079Camera scp079Camera;
			if (!Scp079InteractableBase.TryGetInteractable<Scp079Camera>(reader.ReadUShort(), out scp079Camera))
			{
				return;
			}
			scp079Camera.ApplyAxes(reader);
		}

		public void SpawnObject()
		{
			this._role = base.Role as Scp079Role;
			this._role.TryGetOwner(out this._owner);
			this._role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
			this._role.SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._lostSignalHandler);
		}

		private Scp079Role _role;

		private Scp079CurrentCameraSync _curCamSync;

		private Scp079LostSignalHandler _lostSignalHandler;

		private ReferenceHub _owner;

		private readonly Stopwatch _clientSendLimit = Stopwatch.StartNew();

		private const float ClientSendRate = 15f;
	}
}
