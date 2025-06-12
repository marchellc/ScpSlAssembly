using System;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Interactables.Interobjects;

public class Timed173PryableDoor : PryableDoor
{
	[NonSerialized]
	public readonly Stopwatch Stopwatch = new Stopwatch();

	public float TimeMark = 25f;

	[Tooltip("Automatically opens the gate when the time is over and SCP-173 is spawned.")]
	public bool SmartOpen = true;

	private bool _eventAssigned;

	protected override void Awake()
	{
		base.Awake();
		if (NetworkServer.active)
		{
			CharacterClassManager.OnRoundStarted += this.Stopwatch.Start;
			base.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: true);
			this._eventAssigned = true;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (this._eventAssigned)
		{
			CharacterClassManager.OnRoundStarted -= this.Stopwatch.Start;
		}
	}

	protected override void Update()
	{
		base.Update();
		if (!this.Stopwatch.IsRunning || this.Stopwatch.Elapsed.TotalSeconds < (double)this.TimeMark)
		{
			return;
		}
		this.Stopwatch.Stop();
		base.ServerChangeLock(DoorLockReason.SpecialDoorFeature, newState: false);
		if (!this.SmartOpen)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.GetRoleId() == RoleTypeId.Scp173)
			{
				base.NetworkTargetState = true;
				break;
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}
