using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public abstract class BasicDoorButton : ButtonVariant
{
	[SerializeField]
	private LockReasonIcon _lockIcons;

	private bool _isDestroyed;

	private bool _initialized;

	public override void Init(DoorVariant door)
	{
		base.Init(door);
		this._initialized = true;
		door.OnStateChanged += OnStateChanged;
		door.OnLockChanged += OnLockChanged;
		if (door is IDamageableDoor damageableDoor)
		{
			damageableDoor.OnDestroyedChanged += OnDestroyedChanged;
		}
		this.UpdateMaterials();
	}

	public void TriggerDoorDenied(DoorPermissionFlags flags)
	{
		if (!base.enabled)
		{
			this.OnDenied(flags);
		}
	}

	private void OnDestroyedChanged()
	{
		this._isDestroyed = (base.ParentDoor as IDamageableDoor).IsDestroyed;
		if (this._isDestroyed)
		{
			this.SetAsDestroyed();
		}
		else
		{
			this.RestoreNonDestroyed();
		}
		this.UpdateMaterials();
	}

	private void OnStateChanged()
	{
		if (!this._isDestroyed && base.ParentDoor.ActiveLocks == 0)
		{
			this.SetMoving();
			base.enabled = true;
		}
	}

	private void OnLockChanged()
	{
		if (!this._isDestroyed)
		{
			if (this._lockIcons != null)
			{
				this._lockIcons.UpdateIcon(base.ParentDoor);
			}
			if (base.ParentDoor.IsMoving)
			{
				this.SetMoving();
				base.enabled = true;
			}
			this.UpdateMaterials();
		}
	}

	private void OnEnable()
	{
		if (this._initialized)
		{
			this.UpdateMaterials();
		}
	}

	private void Update()
	{
		this.UpdateMaterials();
	}

	private void UpdateMaterials()
	{
		if (this._isDestroyed)
		{
			base.enabled = false;
		}
		else if (base.ParentDoor.ActiveLocks != 0)
		{
			this.SetLocked();
			base.enabled = false;
		}
		else if (!base.ParentDoor.IsMoving)
		{
			this.SetIdle();
			base.enabled = false;
		}
		else
		{
			this.UpdateMoving();
		}
	}

	protected abstract void SetLocked();

	protected abstract void SetIdle();

	protected abstract void SetMoving();

	protected virtual void UpdateMoving()
	{
	}

	protected virtual void OnDenied(DoorPermissionFlags flags)
	{
	}

	protected virtual void SetAsDestroyed()
	{
	}

	protected virtual void RestoreNonDestroyed()
	{
	}
}
