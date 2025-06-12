using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons;

public class DoorOvercon : StandardOvercon
{
	[SerializeField]
	private Sprite _openSprite;

	[SerializeField]
	private Sprite _closedSprite;

	private SphereCollider _col;

	public DoorVariant Target { get; internal set; }

	private bool IsInvisible => !this.IsCurrentDoorValid();

	private bool IsCurrentDoorValid()
	{
		DoorVariant target = this.Target;
		if (target is IDamageableDoor damageableDoor)
		{
			if (damageableDoor.IsDestroyed)
			{
				return false;
			}
			if (target is CheckpointDoor checkpointDoor)
			{
				if (!checkpointDoor.TargetState)
				{
					return checkpointDoor.GetExactState() <= 0f;
				}
				return false;
			}
		}
		else if (!(target is PryableDoor))
		{
			goto IL_0070;
		}
		DoorLockReason activeLocks = (DoorLockReason)this.Target.ActiveLocks;
		if (activeLocks.HasFlagFast(DoorLockReason.Warhead) || activeLocks.HasFlagFast(DoorLockReason.Isolation))
		{
			return false;
		}
		goto IL_0070;
		IL_0070:
		return true;
	}

	private void LateUpdate()
	{
		base.TargetSprite.sprite = (this.Target.TargetState ? this._openSprite : this._closedSprite);
		bool flag = !this.IsInvisible;
		base.TargetSprite.enabled = flag;
		this._col.enabled = flag;
	}

	protected override void Awake()
	{
		base.Awake();
		this._col = base.GetComponent<SphereCollider>();
	}
}
