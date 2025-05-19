using System;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

public class AlphaWarheadNukesitePanel : NetworkBehaviour
{
	private enum DiodeType
	{
		InProgress,
		BlastDoor,
		OutsideDoor
	}

	public Transform lever;

	[SerializeField]
	private MeshRenderer _ledRenderer;

	[SerializeField]
	private Material _onMat;

	[SerializeField]
	private Material _offMat;

	[SerializeField]
	private Quaternion _enabledLeverRotation;

	[SerializeField]
	private Quaternion _disabledLeverRotation;

	[SyncVar]
	public new bool enabled;

	private float _leverStatus;

	private Material[] _matSet;

	private bool[] _prevMats;

	private bool _anyModified;

	private const string OutsideDoorName = "SURFACE_NUKE";

	public static AlphaWarheadNukesitePanel Singleton;

	private bool _doorFound;

	private DoorNametagExtension _outsideDoor;

	public bool AnyBlastdoorClosed
	{
		get
		{
			foreach (BlastDoor instance in BlastDoor.Instances)
			{
				if (!instance.IsOpen)
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool OutsideDoorOpen
	{
		get
		{
			if (!_doorFound)
			{
				_doorFound = DoorNametagExtension.NamedDoors.TryGetValue("SURFACE_NUKE", out _outsideDoor);
			}
			if (_doorFound)
			{
				return _outsideDoor.TargetDoor.TargetState;
			}
			return false;
		}
	}

	public bool Networkenabled
	{
		get
		{
			return enabled;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref enabled, 1uL, null);
		}
	}

	private void Awake()
	{
		Singleton = this;
		int num = _ledRenderer.sharedMaterials.Length;
		_matSet = new Material[num];
		_prevMats = new bool[num];
		for (int i = 0; i < num; i++)
		{
			_matSet[i] = _offMat;
		}
	}

	public bool AllowChangeLevelState()
	{
		if (!(Math.Abs(_leverStatus) < 0.001f))
		{
			return Math.Abs(_leverStatus - 1f) < 0.001f;
		}
		return true;
	}

	private void SetDiode(DiodeType diode, bool status)
	{
		if (_prevMats[(int)diode] != status)
		{
			_matSet[(int)diode] = (status ? _onMat : _offMat);
			_prevMats[(int)diode] = status;
			_anyModified = true;
		}
	}

	private void Update()
	{
		_anyModified = false;
		SetDiode(DiodeType.InProgress, AlphaWarheadController.InProgress);
		SetDiode(DiodeType.OutsideDoor, OutsideDoorOpen);
		SetDiode(DiodeType.BlastDoor, AnyBlastdoorClosed);
		if (_anyModified)
		{
			_ledRenderer.sharedMaterials = _matSet;
		}
		_leverStatus += (enabled ? 0.04f : (-0.04f));
		_leverStatus = Mathf.Clamp01(_leverStatus);
		lever.localRotation = Quaternion.Slerp(_disabledLeverRotation, _enabledLeverRotation, _leverStatus);
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
			writer.WriteBool(enabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(enabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref enabled, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref enabled, null, reader.ReadBool());
		}
	}
}
