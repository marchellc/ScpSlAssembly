using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class Timed173PryableDoor : PryableDoor
	{
		protected override void Awake()
		{
			base.Awake();
			if (NetworkServer.active)
			{
				CharacterClassManager.OnRoundStarted += this.Stopwatch.Start;
				base.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
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
			if (!this.Stopwatch.IsRunning)
			{
				return;
			}
			if (this.Stopwatch.Elapsed.TotalSeconds < (double)this.TimeMark)
			{
				return;
			}
			this.Stopwatch.Stop();
			base.ServerChangeLock(DoorLockReason.SpecialDoorFeature, false);
			if (!this.SmartOpen)
			{
				return;
			}
			using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.GetRoleId() == RoleTypeId.Scp173)
					{
						base.NetworkTargetState = true;
						break;
					}
				}
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		[NonSerialized]
		public readonly Stopwatch Stopwatch = new Stopwatch();

		public float TimeMark = 25f;

		[Tooltip("Automatically opens the gate when the time is over and SCP-173 is spawned.")]
		public bool SmartOpen = true;

		private bool _eventAssigned;
	}
}
