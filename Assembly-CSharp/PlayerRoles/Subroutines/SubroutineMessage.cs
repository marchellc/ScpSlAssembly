using System;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.Subroutines;

public readonly struct SubroutineMessage : NetworkMessage
{
	private readonly int _subroutineIndex;

	private readonly bool? _isConfirmation;

	private readonly SubroutineBase _subroutine;

	private readonly ReferenceHub _target;

	private readonly RoleTypeId _role;

	private readonly NetworkReaderPooled _reader;

	public SubroutineMessage(SubroutineBase subroutine, bool isConfirmation)
	{
		this._reader = null;
		this._isConfirmation = isConfirmation;
		this._subroutine = subroutine;
		this._subroutineIndex = subroutine.SyncIndex;
		this._role = subroutine.Role.RoleTypeId;
		subroutine.Role.TryGetOwner(out this._target);
	}

	public SubroutineMessage(NetworkReader reader)
	{
		this._subroutine = null;
		this._isConfirmation = null;
		this._subroutineIndex = reader.ReadByte();
		if (this._subroutineIndex == 0)
		{
			this._reader = null;
			this._target = null;
			this._role = RoleTypeId.None;
			return;
		}
		this._target = reader.ReadReferenceHub();
		this._role = reader.ReadRoleType();
		int num = reader.ReadByte();
		if (num == 255)
		{
			num += reader.ReadUShort();
		}
		this._reader = NetworkReaderPool.Get(reader.ReadBytesSegment(num));
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteByte((byte)this._subroutineIndex);
		if (this._subroutineIndex != 0)
		{
			writer.WriteReferenceHub(this._target);
			writer.WriteRoleType(this._role);
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			if (this._isConfirmation == true)
			{
				this._subroutine.ServerWriteRpc(networkWriterPooled);
			}
			else
			{
				this._subroutine.ClientWriteCmd(networkWriterPooled);
			}
			int num = networkWriterPooled.Position;
			if (num > 65790)
			{
				num = 0;
			}
			writer.WriteByte((byte)Math.Min(num, 255));
			if (num >= 255)
			{
				writer.WriteUShort((ushort)(num - 255));
			}
			writer.WriteBytes(networkWriterPooled.buffer, 0, num);
			networkWriterPooled.Dispose();
		}
	}

	public void ServerApplyTrigger(NetworkConnection conn)
	{
		if (this._subroutineIndex == 0)
		{
			return;
		}
		if (ReferenceHub.TryGetHub(conn, out var hub))
		{
			if (hub.isLocalPlayer)
			{
				hub = this._target;
			}
			this.Apply(hub, server: true);
		}
		this._reader.Dispose();
	}

	public void ClientApplyConfirmation()
	{
		if (this._subroutineIndex != 0)
		{
			if (this._target != null)
			{
				this.Apply(this._target, server: false);
			}
			this._reader.Dispose();
		}
	}

	private void Apply(ReferenceHub hub, bool server)
	{
		if (!(hub.roleManager.CurrentRole is ISubroutinedRole subroutinedRole) || hub.GetRoleId() != this._role)
		{
			return;
		}
		int num = this._subroutineIndex - 1;
		if (num < 0 || num >= subroutinedRole.SubroutineModule.AllSubroutines.Length)
		{
			return;
		}
		SubroutineBase subroutineBase = subroutinedRole.SubroutineModule.AllSubroutines[num];
		if (server)
		{
			subroutineBase.ServerProcessCmd(this._reader);
			return;
		}
		try
		{
			subroutineBase.ClientProcessRpc(this._reader);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
