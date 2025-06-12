using System;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects;

[Serializable]
public class CheckpointSequenceController
{
	private CheckpointDoor _checkpoint;

	[field: SerializeField]
	public float OpenLoopTime { get; set; }

	[field: SerializeField]
	public float WarningTime { get; set; }

	public float RemainingTime { get; set; }

	private bool AnyOpen
	{
		get
		{
			DoorVariant[] subDoors = this._checkpoint.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i].TargetState)
				{
					return true;
				}
			}
			return this._checkpoint.TargetState;
		}
	}

	private bool AnyClosed
	{
		get
		{
			DoorVariant[] subDoors = this._checkpoint.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (!subDoors[i].TargetState)
				{
					return true;
				}
			}
			return !this._checkpoint.TargetState;
		}
	}

	private DoorLockReason LockReasons
	{
		get
		{
			ushort num = 0;
			DoorVariant[] subDoors = this._checkpoint.SubDoors;
			foreach (DoorVariant doorVariant in subDoors)
			{
				num |= doorVariant.ActiveLocks;
			}
			return (DoorLockReason)(num | this._checkpoint.ActiveLocks);
		}
	}

	private bool CanClose => DoorLockUtils.GetMode(this.LockReasons).HasFlagFast(DoorLockMode.CanClose);

	public void Init(CheckpointDoor checkpoint)
	{
		this._checkpoint = checkpoint;
	}

	public CheckpointDoor.SequenceState UpdateSequence()
	{
		CheckpointDoor.SequenceState curSequence = this._checkpoint.CurSequence;
		if (curSequence == CheckpointDoor.SequenceState.Idle)
		{
			return this.UpdateIdle();
		}
		if (this.AnyClosed)
		{
			this.SetStateAll(targetState: false);
			return CheckpointDoor.SequenceState.Idle;
		}
		return curSequence switch
		{
			CheckpointDoor.SequenceState.Granted => this.UpdateGranted(), 
			CheckpointDoor.SequenceState.OpenLoop => this.UpdateOpenLoop(), 
			CheckpointDoor.SequenceState.ClosingWarning => this.UpdateClosingWarning(), 
			_ => throw new InvalidOperationException("Unable to update checkpoint! Invalid sequence: " + curSequence), 
		};
	}

	private CheckpointDoor.SequenceState UpdateIdle()
	{
		if (!this.AnyOpen)
		{
			return CheckpointDoor.SequenceState.Idle;
		}
		this.SetStateAll(targetState: true);
		return CheckpointDoor.SequenceState.Granted;
	}

	private CheckpointDoor.SequenceState UpdateGranted()
	{
		DoorVariant[] subDoors = this._checkpoint.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (!subDoors[i].IsConsideredOpen())
			{
				return CheckpointDoor.SequenceState.Granted;
			}
		}
		this.RemainingTime = this.OpenLoopTime;
		return CheckpointDoor.SequenceState.OpenLoop;
	}

	private CheckpointDoor.SequenceState UpdateOpenLoop()
	{
		this.RemainingTime -= Time.deltaTime;
		if (this.RemainingTime > 0f || !this.CanClose)
		{
			return CheckpointDoor.SequenceState.OpenLoop;
		}
		this.RemainingTime = this.WarningTime;
		return CheckpointDoor.SequenceState.ClosingWarning;
	}

	private CheckpointDoor.SequenceState UpdateClosingWarning()
	{
		if (!this.CanClose)
		{
			return CheckpointDoor.SequenceState.OpenLoop;
		}
		this.RemainingTime -= Time.deltaTime;
		if (this.RemainingTime > 0f)
		{
			return CheckpointDoor.SequenceState.ClosingWarning;
		}
		this.SetStateAll(targetState: false);
		return CheckpointDoor.SequenceState.Idle;
	}

	private void SetStateAll(bool targetState)
	{
		DoorVariant[] subDoors = this._checkpoint.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			subDoors[i].NetworkTargetState = targetState;
		}
		this._checkpoint.NetworkTargetState = targetState;
	}
}
