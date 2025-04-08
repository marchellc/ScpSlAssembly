using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

public class AlphaWarheadNukesitePanel : NetworkBehaviour
{
	public bool AnyBlastdoorClosed
	{
		get
		{
			using (HashSet<BlastDoor>.Enumerator enumerator = BlastDoor.Instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsOpen)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	private void Awake()
	{
		AlphaWarheadNukesitePanel.Singleton = this;
		int num = this._ledRenderer.sharedMaterials.Length;
		this._matSet = new Material[num];
		this._prevMats = new bool[num];
		for (int i = 0; i < num; i++)
		{
			this._matSet[i] = this._offMat;
		}
	}

	public bool AllowChangeLevelState()
	{
		return Math.Abs(this._leverStatus) < 0.001f || Math.Abs(this._leverStatus - 1f) < 0.001f;
	}

	private bool OutsideDoorOpen
	{
		get
		{
			if (!this._doorFound)
			{
				this._doorFound = DoorNametagExtension.NamedDoors.TryGetValue("SURFACE_NUKE", out this._outsideDoor);
			}
			return this._doorFound && this._outsideDoor.TargetDoor.TargetState;
		}
	}

	private void SetDiode(AlphaWarheadNukesitePanel.DiodeType diode, bool status)
	{
		if (this._prevMats[(int)diode] == status)
		{
			return;
		}
		this._matSet[(int)diode] = (status ? this._onMat : this._offMat);
		this._prevMats[(int)diode] = status;
		this._anyModified = true;
	}

	private void Update()
	{
		this._anyModified = false;
		this.SetDiode(AlphaWarheadNukesitePanel.DiodeType.InProgress, AlphaWarheadController.InProgress);
		this.SetDiode(AlphaWarheadNukesitePanel.DiodeType.OutsideDoor, this.OutsideDoorOpen);
		this.SetDiode(AlphaWarheadNukesitePanel.DiodeType.BlastDoor, this.AnyBlastdoorClosed);
		if (this._anyModified)
		{
			this._ledRenderer.sharedMaterials = this._matSet;
		}
		this._leverStatus += (this.enabled ? 0.04f : (-0.04f));
		this._leverStatus = Mathf.Clamp01(this._leverStatus);
		this.lever.localRotation = Quaternion.Slerp(this._disabledLeverRotation, this._enabledLeverRotation, this._leverStatus);
	}

	public override bool Weaved()
	{
		return true;
	}

	public bool Networkenabled
	{
		get
		{
			return this.enabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.enabled, 1UL, null);
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.enabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.enabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.enabled, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.enabled, null, reader.ReadBool());
		}
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

	private enum DiodeType
	{
		InProgress,
		BlastDoor,
		OutsideDoor
	}
}
