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

	[SerializeField]
	private string _commandName;

	[SerializeField]
	private Scp079Camera _camera;

	private ushort _clientSyncId;

	public override string CommandName => "Camera" + _commandName;

	public string NetworkLabel
	{
		get
		{
			return Label;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref Label, 32uL, SetLabel);
		}
	}

	public RoomIdentifier NetworkRoom
	{
		get
		{
			return Room;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref Room, 64uL, SetRoom);
		}
	}

	public Vector2 NetworkVerticalConstraint
	{
		get
		{
			return VerticalConstraint;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref VerticalConstraint, 128uL, SetVerticalConstraint);
		}
	}

	public Vector2 NetworkHorizontalConstraint
	{
		get
		{
			return HorizontalConstraint;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref HorizontalConstraint, 256uL, SetHorizontalConstraint);
		}
	}

	public Vector2 NetworkZoomConstraint
	{
		get
		{
			return ZoomConstraint;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref ZoomConstraint, 512uL, SetZoomConstraint);
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
		_camera.SyncId = (ushort)Scp079InteractableBase.InstancesCount;
		Scp079InteractableBase.OrderedInstances.Add(_camera);
		_camera.Position = base.transform.position;
		NetworkLabel = _camera.Label;
		NetworkVerticalConstraint = _camera.VerticalAxis.Constraints;
		NetworkHorizontalConstraint = _camera.HorizontalAxis.Constraints;
		NetworkZoomConstraint = _camera.ZoomAxis.Constraints;
		SetRoom(null, null);
		NetworkRoom = _camera.Room;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active)
		{
			_camera.SyncId = _clientSyncId;
			int num = Mathf.Max(0, _clientSyncId - Scp079InteractableBase.OrderedInstances.Count);
			for (int i = 0; i < num; i++)
			{
				Scp079InteractableBase.OrderedInstances.Add(null);
			}
			Scp079InteractableBase.OrderedInstances[_clientSyncId - 1] = _camera;
			Scp079InteractableBase.InstancesCount = Scp079InteractableBase.OrderedInstances.Count;
			if (CurrentOvercon == null && OverconManager.Singleton != null && OverconManager.Singleton.TryGetComponent<CameraOverconRenderer>(out var component))
			{
				component.SpawnOvercon(Scp079Hud.Instance.CurrentCamera, _camera);
			}
		}
	}

	public override void OnSerialize(NetworkWriter writer, bool initialState)
	{
		base.OnSerialize(writer, initialState);
		if (initialState)
		{
			writer.WriteUShort(_camera.SyncId);
		}
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		if (initialState)
		{
			_clientSyncId = reader.ReadUShort();
		}
	}

	protected override void UpdatePositionClient(bool teleport = false)
	{
		base.UpdatePositionClient(teleport);
		_camera.Position = base.transform.position;
		if (!(CurrentOvercon == null))
		{
			if (CurrentOvercon.Target == _camera && CurrentOvercon.gameObject.activeSelf)
			{
				CurrentOvercon.Position = _camera.Position;
			}
			else
			{
				CurrentOvercon = null;
			}
		}
	}

	private void SetLabel(string _, string newLabel)
	{
		_camera.Label = newLabel;
	}

	private void SetRoom(RoomIdentifier _, RoomIdentifier newRoom)
	{
		if (newRoom != null)
		{
			_camera.Room = newRoom;
			return;
		}
		if (!base.transform.position.TryGetRoom(out var room))
		{
			_camera.Room = room;
			return;
		}
		RoomIdentifier roomIdentifier = null;
		float num = 0f;
		foreach (RoomIdentifier allRoomIdentifier in RoomIdentifier.AllRoomIdentifiers)
		{
			float sqrMagnitude = (_camera.Position - allRoomIdentifier.gameObject.transform.position).sqrMagnitude;
			if (roomIdentifier == null || sqrMagnitude < num)
			{
				roomIdentifier = allRoomIdentifier;
				num = sqrMagnitude;
			}
		}
		_camera.Room = roomIdentifier;
	}

	private void SetVerticalConstraint(Vector2 _, Vector2 newConstraint)
	{
		_camera.VerticalAxis.Constraints = newConstraint;
	}

	private void SetHorizontalConstraint(Vector2 _, Vector2 newConstraint)
	{
		_camera.HorizontalAxis.Constraints = newConstraint;
	}

	private void SetZoomConstraint(Vector2 _, Vector2 newConstraint)
	{
		_camera.ZoomAxis.Constraints = newConstraint;
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
			writer.WriteString(Label);
			writer.WriteRoomIdentifier(Room);
			writer.WriteVector2(VerticalConstraint);
			writer.WriteVector2(HorizontalConstraint);
			writer.WriteVector2(ZoomConstraint);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteString(Label);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteRoomIdentifier(Room);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteVector2(VerticalConstraint);
		}
		if ((base.syncVarDirtyBits & 0x100L) != 0L)
		{
			writer.WriteVector2(HorizontalConstraint);
		}
		if ((base.syncVarDirtyBits & 0x200L) != 0L)
		{
			writer.WriteVector2(ZoomConstraint);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref Label, SetLabel, reader.ReadString());
			GeneratedSyncVarDeserialize(ref Room, SetRoom, reader.ReadRoomIdentifier());
			GeneratedSyncVarDeserialize(ref VerticalConstraint, SetVerticalConstraint, reader.ReadVector2());
			GeneratedSyncVarDeserialize(ref HorizontalConstraint, SetHorizontalConstraint, reader.ReadVector2());
			GeneratedSyncVarDeserialize(ref ZoomConstraint, SetZoomConstraint, reader.ReadVector2());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref Label, SetLabel, reader.ReadString());
		}
		if ((num & 0x40L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref Room, SetRoom, reader.ReadRoomIdentifier());
		}
		if ((num & 0x80L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref VerticalConstraint, SetVerticalConstraint, reader.ReadVector2());
		}
		if ((num & 0x100L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref HorizontalConstraint, SetHorizontalConstraint, reader.ReadVector2());
		}
		if ((num & 0x200L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref ZoomConstraint, SetZoomConstraint, reader.ReadVector2());
		}
	}
}
