using System;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class DoorOvercon : StandardOvercon
	{
		public DoorVariant Target { get; internal set; }

		private bool IsInvisible
		{
			get
			{
				return !this.IsCurrentDoorValid();
			}
		}

		private bool IsCurrentDoorValid()
		{
			DoorVariant target = this.Target;
			IDamageableDoor damageableDoor = target as IDamageableDoor;
			if (damageableDoor != null)
			{
				if (damageableDoor.IsDestroyed)
				{
					return false;
				}
				CheckpointDoor checkpointDoor = target as CheckpointDoor;
				if (checkpointDoor != null)
				{
					return !checkpointDoor.TargetState && checkpointDoor.GetExactState() <= 0f;
				}
			}
			else if (!(target is PryableDoor))
			{
				return true;
			}
			DoorLockReason activeLocks = (DoorLockReason)this.Target.ActiveLocks;
			if (activeLocks.HasFlagFast(DoorLockReason.Warhead) || activeLocks.HasFlagFast(DoorLockReason.Isolation))
			{
				return false;
			}
			return true;
		}

		private void LateUpdate()
		{
			this.TargetSprite.sprite = (this.Target.TargetState ? this._openSprite : this._closedSprite);
			bool flag = !this.IsInvisible;
			this.TargetSprite.enabled = flag;
			this._col.enabled = flag;
		}

		protected override void Awake()
		{
			base.Awake();
			this._col = base.GetComponent<SphereCollider>();
		}

		[SerializeField]
		private Sprite _openSprite;

		[SerializeField]
		private Sprite _closedSprite;

		private SphereCollider _col;
	}
}
