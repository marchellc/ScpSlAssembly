using GameCore;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class VersionCheck : NetworkBehaviour
{
	private void Start()
	{
		if (NetworkServer.active && (!Version.AlwaysAcceptReleaseBuilds || Version.BuildType != 0))
		{
			if (Version.ExtendedVersionCheckNeeded)
			{
				TargetCheckExactVersion(base.connectionToClient, Version.VersionString);
			}
			else
			{
				TargetCheckVersion(base.connectionToClient, Version.Major, Version.Minor, Version.Revision);
			}
		}
	}

	[TargetRpc]
	private void TargetCheckVersion(NetworkConnection conn, byte major, byte minor, byte revision)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		NetworkWriterExtensions.WriteByte(writer, major);
		NetworkWriterExtensions.WriteByte(writer, minor);
		NetworkWriterExtensions.WriteByte(writer, revision);
		SendTargetRPCInternal(conn, "System.Void VersionCheck::TargetCheckVersion(Mirror.NetworkConnection,System.Byte,System.Byte,System.Byte)", 878887720, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[TargetRpc]
	private void TargetCheckExactVersion(NetworkConnection conn, string version)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteString(version);
		SendTargetRPCInternal(conn, "System.Void VersionCheck::TargetCheckExactVersion(Mirror.NetworkConnection,System.String)", 87233244, writer, 0);
		NetworkWriterPool.Return(writer);
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
		}
		else
		{
			((VersionCheck)obj).UserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte(null, NetworkReaderExtensions.ReadByte(reader), NetworkReaderExtensions.ReadByte(reader), NetworkReaderExtensions.ReadByte(reader));
		}
	}

	protected void UserCode_TargetCheckExactVersion__NetworkConnection__String(NetworkConnection conn, string version)
	{
	}

	protected static void InvokeUserCode_TargetCheckExactVersion__NetworkConnection__String(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetCheckExactVersion called on server.");
		}
		else
		{
			((VersionCheck)obj).UserCode_TargetCheckExactVersion__NetworkConnection__String(null, reader.ReadString());
		}
	}

	static VersionCheck()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(VersionCheck), "System.Void VersionCheck::TargetCheckVersion(Mirror.NetworkConnection,System.Byte,System.Byte,System.Byte)", InvokeUserCode_TargetCheckVersion__NetworkConnection__Byte__Byte__Byte);
		RemoteProcedureCalls.RegisterRpc(typeof(VersionCheck), "System.Void VersionCheck::TargetCheckExactVersion(Mirror.NetworkConnection,System.String)", InvokeUserCode_TargetCheckExactVersion__NetworkConnection__String);
	}
}
