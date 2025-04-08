using System;
using GameCore;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class VersionCheck : NetworkBehaviour
{
	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (global::GameCore.Version.AlwaysAcceptReleaseBuilds && global::GameCore.Version.BuildType == global::GameCore.Version.VersionType.Release)
		{
			return;
		}
		if (global::GameCore.Version.ExtendedVersionCheckNeeded)
		{
			this.TargetCheckExactVersion(base.connectionToClient, global::GameCore.Version.VersionString);
			return;
		}
		this.TargetCheckVersion(base.connectionToClient, global::GameCore.Version.Major, global::GameCore.Version.Minor, global::GameCore.Version.Revision);
	}

	[TargetRpc]
	private void TargetCheckVersion(NetworkConnection conn, byte major, byte minor, byte revision)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteByte(major);
		networkWriterPooled.WriteByte(minor);
		networkWriterPooled.WriteByte(revision);
		this.SendTargetRPCInternal(conn, "System.Void VersionCheck::TargetCheckVersion(Mirror.NetworkConnection,System.Byte,System.Byte,System.Byte)", 878887720, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[TargetRpc]
	private void TargetCheckExactVersion(NetworkConnection conn, string version)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteString(version);
		this.SendTargetRPCInternal(conn, "System.Void VersionCheck::TargetCheckExactVersion(Mirror.NetworkConnection,System.String)", 87233244, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte(NetworkConnection conn, byte major, byte minor, byte revision)
	{
	}

	protected static void InvokeUserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetCheckVersion called on server.");
			return;
		}
		((VersionCheck)obj).UserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte(null, reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
	}

	protected void UserCode_TargetCheckExactVersion__NetworkConnection__String(NetworkConnection conn, string version)
	{
	}

	protected static void InvokeUserCode_TargetCheckExactVersion__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetCheckExactVersion called on server.");
			return;
		}
		((VersionCheck)obj).UserCode_TargetCheckExactVersion__NetworkConnection__String(null, reader.ReadString());
	}

	static VersionCheck()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(VersionCheck), "System.Void VersionCheck::TargetCheckVersion(Mirror.NetworkConnection,System.Byte,System.Byte,System.Byte)", new RemoteCallDelegate(VersionCheck.InvokeUserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte));
		RemoteProcedureCalls.RegisterRpc(typeof(VersionCheck), "System.Void VersionCheck::TargetCheckExactVersion(Mirror.NetworkConnection,System.String)", new RemoteCallDelegate(VersionCheck.InvokeUserCode_TargetCheckExactVersion__NetworkConnection__String));
	}
}
