using System;
using System.Runtime.InteropServices;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;

namespace AdminToys;

[RequireComponent(typeof(Scp079InteractableBase))]
public class Scp079CameraToy : AdminToyBase
{
	[NonSerialized]
	[SyncVar(hook = "SetLabel")]
	public string Label;

	[NonSerialized]
	[SyncVar(hook = "SetRoom")]
	public RoomIdentifier Room;

	[NonSerialized]
	[SyncVar(hook = "SetVerticalConstraint")]
	public Vector2 VerticalConstraint;

	[NonSerialized]
	[SyncVar(hook = "SetHorizontalConstraint")]
	public Vector2 HorizontalConstraint;

	[NonSerialized]
	[SyncVar(hook = "SetZoomConstraint")]
	public Vector2 ZoomConstraint;

	[NonSerialized]
	public CameraOvercon CurrentOvercon;

	public FacilityZone ZoneIcon;

	public Scp079Camera Camera;

	[SerializeField]
	private string _commandName;

	private ushort _clientSyncId;

	public override string CommandName => "Camera" + this._commandName;

	public string NetworkLabel
	{
		get
		{
			return this.Label;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Label, 32uL, SetLabel);
		}
	}

	public RoomIdentifier NetworkRoom
	{
		get
		{
			return this.Room;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Room, 64uL, SetRoom);
		}
	}

	public Vector2 NetworkVerticalConstraint
	{
		get
		{
			return this.VerticalConstraint;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.VerticalConstraint, 128uL, SetVerticalConstraint);
		}
	}

	public Vector2 NetworkHorizontalConstraint
	{
		get
		{
			return this.HorizontalConstraint;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.HorizontalConstraint, 256uL, SetHorizontalConstraint);
		}
	}

	public Vector2 NetworkZoomConstraint
	{
		get
		{
			return this.ZoomConstraint;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ZoomConstraint, 512uL, SetZoomConstraint);
		}
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		string[] array = arguments.Array;
		float result;
		float num = ((array.Length > 2 && float.TryParse(array[2], out result)) ? result : 1f);
		base.transform.SetPositionAndRotation(admin.PlayerCameraReference.position, admin.PlayerCameraReference.rotation);
		base.transform.localScale = Vector3.one * num;
		base.NetworkScale = base.transform.localScale;
		base.OnSpawned(admin, arguments);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		Scp079InteractableBase.InstancesCount++;
		this.Camera.SyncId = (ushort)Scp079InteractableBase.InstancesCount;
		Scp079InteractableBase.OrderedInstances.Add(this.Camera);
		this.Camera.Position = base.transform.position;
		this.NetworkLabel = this.Camera.Label;
		this.NetworkVerticalConstraint = this.Camera.VerticalAxis.Constraints;
		this.NetworkHorizontalConstraint = this.Camera.HorizontalAxis.Constraints;
		this.NetworkZoomConstraint = this.Camera.ZoomAxis.Constraints;
		this.SetRoom(null, this.Room);
		this.NetworkRoom = this.Camera.Room;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active)
		{
			this.Camera.SyncId = this._clientSyncId;
			int num = Mathf.Max(0, this._clientSyncId - Scp079InteractableBase.OrderedInstances.Count);
			for (int i = 0; i < num; i++)
			{
				Scp079InteractableBase.OrderedInstances.Add(null);
			}
			Scp079InteractableBase.OrderedInstances[this._clientSyncId - 1] = this.Camera;
			Scp079InteractableBase.InstancesCount = Scp079InteractableBase.OrderedInstances.Count;
			if (this.CurrentOvercon == null && OverconManager.Singleton != null && OverconManager.Singleton.TryGetComponent<CameraOverconRenderer>(out var component))
			{
				component.SpawnOvercon(Scp079Hud.Instance.CurrentCamera, this.Camera);
			}
		}
	}

	public override void OnSerialize(NetworkWriter writer, bool initialState)
	{
		base.OnSerialize(writer, initialState);
		if (initialState)
		{
			writer.WriteUShort(this.Camera.SyncId);
		}
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		if (initialState)
		{
			this._clientSyncId = reader.ReadUShort();
		}
	}

	protected override void UpdatePositionClient(bool teleport = false)
	{
		base.UpdatePositionClient(teleport);
		this.Camera.Position = base.transform.position;
		if (this.CurrentOvercon == null)
		{
			return;
		}
		if (this.CurrentOvercon.Target == this.Camera && this.CurrentOvercon.gameObject.activeSelf)
		{
			this.CurrentOvercon.Position = this.Camera.Position;
			if (Scp079Hud.Instance != null && Scp079Hud.Instance.CurrentCamera != null)
			{
				this.CurrentOvercon.Rescale(Scp079Hud.Instance.CurrentCamera);
			}
		}
		else
		{
			this.CurrentOvercon = null;
		}
	}

	private void SetLabel(string _, string newLabel)
	{
		this.Camera.Label = newLabel;
	}

	private void SetRoom(RoomIdentifier _, RoomIdentifier newRoom)
	{
		if (newRoom != null)
		{
			this.Camera.Room = newRoom;
			return;
		}
		if (base.transform.position.TryGetRoom(out var room))
		{
			this.Camera.Room = room;
			return;
		}
		RoomIdentifier roomIdentifier = null;
		float num = 0f;
		foreach (RoomIdentifier allRoomIdentifier in RoomIdentifier.AllRoomIdentifiers)
		{
			float sqrMagnitude = (this.Camera.Position - allRoomIdentifier.gameObject.transform.position).sqrMagnitude;
			if (roomIdentifier == null || sqrMagnitude < num)
			{
				roomIdentifier = allRoomIdentifier;
				num = sqrMagnitude;
			}
		}
		this.Camera.Room = roomIdentifier;
	}

	private void SetVerticalConstraint(Vector2 _, Vector2 newConstraint)
	{
		this.Camera.VerticalAxis.Constraints = newConstraint;
	}

	private void SetHorizontalConstraint(Vector2 _, Vector2 newConstraint)
	{
		this.Camera.HorizontalAxis.Constraints = newConstraint;
	}

	private void SetZoomConstraint(Vector2 _, Vector2 newConstraint)
	{
		this.Camera.ZoomAxis.Constraints = newConstraint;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteString(this.Label);
			writer.WriteRoomIdentifier(this.Room);
			writer.WriteVector2(this.VerticalConstraint);
			writer.WriteVector2(this.HorizontalConstraint);
			writer.WriteVector2(this.ZoomConstraint);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteString(this.Label);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteRoomIdentifier(this.Room);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteVector2(this.VerticalConstraint);
		}
		if ((base.syncVarDirtyBits & 0x100L) != 0L)
		{
			writer.WriteVector2(this.HorizontalConstraint);
		}
		if ((base.syncVarDirtyBits & 0x200L) != 0L)
		{
			writer.WriteVector2(this.ZoomConstraint);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Label, SetLabel, reader.ReadString());
			base.GeneratedSyncVarDeserialize(ref this.Room, SetRoom, reader.ReadRoomIdentifier());
			base.GeneratedSyncVarDeserialize(ref this.VerticalConstraint, SetVerticalConstraint, reader.ReadVector2());
			base.GeneratedSyncVarDeserialize(ref this.HorizontalConstraint, SetHorizontalConstraint, reader.ReadVector2());
			base.GeneratedSyncVarDeserialize(ref this.ZoomConstraint, SetZoomConstraint, reader.ReadVector2());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Label, SetLabel, reader.ReadString());
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Room, SetRoom, reader.ReadRoomIdentifier());
		}
		if ((num & 0x80L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.VerticalConstraint, SetVerticalConstraint, reader.ReadVector2());
		}
		if ((num & 0x100L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.HorizontalConstraint, SetHorizontalConstraint, reader.ReadVector2());
		}
		if ((num & 0x200L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ZoomConstraint, SetZoomConstraint, reader.ReadVector2());
		}
	}
}
