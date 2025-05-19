using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class AspectRatioSync : NetworkBehaviour
{
	private static float _defaultCameraFieldOfView;

	[CompilerGenerated]
	[SyncVar]
	private float _003CXScreenEdge_003Ek__BackingField;

	private int _savedWidth;

	private int _savedHeight;

	public static float YScreenEdge { get; private set; }

	public float XScreenEdge
	{
		[CompilerGenerated]
		get
		{
			return _003CXScreenEdge_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			Network_003CXScreenEdge_003Ek__BackingField = value;
		}
	} = 35f;

	public float XplusY { get; private set; } = 70f;

	public float AspectRatio { get; private set; } = 1f;

	public float Network_003CXScreenEdge_003Ek__BackingField
	{
		get
		{
			return XScreenEdge;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref XScreenEdge, 1uL, null);
		}
	}

	public static event Action OnAspectRatioChanged;

	private void Start()
	{
		if (base.isLocalPlayer)
		{
			Camera component = GetComponent<ReferenceHub>().PlayerCameraReference.GetComponent<Camera>();
			_defaultCameraFieldOfView = ((component == null) ? 70f : component.fieldOfView);
			YScreenEdge = _defaultCameraFieldOfView / 2f;
		}
	}

	private void UpdateAspectRatio()
	{
		_savedWidth = Screen.width;
		_savedHeight = Screen.height;
		float aspectRatio = (float)Screen.width / (float)Screen.height;
		CmdSetAspectRatio(aspectRatio);
	}

	private void FixedUpdate()
	{
	}

	[Command(channel = 4)]
	private void CmdSetAspectRatio(float aspectRatio)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteFloat(aspectRatio);
		SendCommandInternal("System.Void AspectRatioSync::CmdSetAspectRatio(System.Single)", -837572567, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	static AspectRatioSync()
	{
		YScreenEdge = 35f;
		RemoteProcedureCalls.RegisterCommand(typeof(AspectRatioSync), "System.Void AspectRatioSync::CmdSetAspectRatio(System.Single)", InvokeUserCode_CmdSetAspectRatio__Single, requiresAuthority: true);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdSetAspectRatio__Single(float aspectRatio)
	{
		if (aspectRatio < 1f)
		{
			aspectRatio = 1f;
		}
		AspectRatio = aspectRatio;
		float num = Mathf.Tan(_defaultCameraFieldOfView * (MathF.PI / 180f) * 0.5f);
		Network_003CXScreenEdge_003Ek__BackingField = Mathf.Atan(num * aspectRatio) * 57.29578f;
		XplusY = XScreenEdge + YScreenEdge;
	}

	protected static void InvokeUserCode_CmdSetAspectRatio__Single(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetAspectRatio called on client.");
		}
		else
		{
			((AspectRatioSync)obj).UserCode_CmdSetAspectRatio__Single(reader.ReadFloat());
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(XScreenEdge);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(XScreenEdge);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref XScreenEdge, null, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref XScreenEdge, null, reader.ReadFloat());
		}
	}
}
