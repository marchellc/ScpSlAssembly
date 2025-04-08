using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class AspectRatioSync : NetworkBehaviour
{
	public static event Action OnAspectRatioChanged;

	public static float YScreenEdge { get; private set; } = 35f;

	public float XScreenEdge
	{
		[CompilerGenerated]
		get
		{
			return this.<XScreenEdge>k__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			this.Network<XScreenEdge>k__BackingField = value;
		}
	}

	public float XplusY { get; private set; }

	public float AspectRatio { get; private set; }

	private void Start()
	{
		if (!base.isLocalPlayer)
		{
			return;
		}
		Camera component = base.GetComponent<ReferenceHub>().PlayerCameraReference.GetComponent<Camera>();
		AspectRatioSync._defaultCameraFieldOfView = ((component == null) ? 70f : component.fieldOfView);
		AspectRatioSync.YScreenEdge = AspectRatioSync._defaultCameraFieldOfView / 2f;
	}

	private void UpdateAspectRatio()
	{
		this._savedWidth = Screen.width;
		this._savedHeight = Screen.height;
		float num = (float)Screen.width / (float)Screen.height;
		this.CmdSetAspectRatio(num);
	}

	private void FixedUpdate()
	{
	}

	[Command(channel = 4)]
	private void CmdSetAspectRatio(float aspectRatio)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteFloat(aspectRatio);
		base.SendCommandInternal("System.Void AspectRatioSync::CmdSetAspectRatio(System.Single)", -837572567, networkWriterPooled, 4, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public AspectRatioSync()
	{
		this.<XScreenEdge>k__BackingField = 35f;
		this.XplusY = 70f;
		this.AspectRatio = 1f;
		base..ctor();
	}

	static AspectRatioSync()
	{
		RemoteProcedureCalls.RegisterCommand(typeof(AspectRatioSync), "System.Void AspectRatioSync::CmdSetAspectRatio(System.Single)", new RemoteCallDelegate(AspectRatioSync.InvokeUserCode_CmdSetAspectRatio__Single), true);
	}

	public override bool Weaved()
	{
		return true;
	}

	public float Network<XScreenEdge>k__BackingField
	{
		get
		{
			return this.<XScreenEdge>k__BackingField;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<float>(value, ref this.<XScreenEdge>k__BackingField, 1UL, null);
		}
	}

	protected void UserCode_CmdSetAspectRatio__Single(float aspectRatio)
	{
		if (aspectRatio < 1f)
		{
			aspectRatio = 1f;
		}
		this.AspectRatio = aspectRatio;
		float num = Mathf.Tan(AspectRatioSync._defaultCameraFieldOfView * 0.017453292f * 0.5f);
		this.XScreenEdge = Mathf.Atan(num * aspectRatio) * 57.29578f;
		this.XplusY = this.XScreenEdge + AspectRatioSync.YScreenEdge;
	}

	protected static void InvokeUserCode_CmdSetAspectRatio__Single(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetAspectRatio called on client.");
			return;
		}
		((AspectRatioSync)obj).UserCode_CmdSetAspectRatio__Single(reader.ReadFloat());
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this.<XScreenEdge>k__BackingField);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteFloat(this.<XScreenEdge>k__BackingField);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.<XScreenEdge>k__BackingField, null, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.<XScreenEdge>k__BackingField, null, reader.ReadFloat());
		}
	}

	private static float _defaultCameraFieldOfView;

	private int _savedWidth;

	private int _savedHeight;
}
