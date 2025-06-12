using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class SimpleButton : BasicDoorButton
{
	[SerializeField]
	private Material _openMat;

	[SerializeField]
	private Material _closedMat;

	[SerializeField]
	private Material _movingMat;

	[SerializeField]
	private Material _lockedMat;

	[SerializeField]
	private MeshRenderer _renderer;

	protected override void SetMoving()
	{
		this._renderer.sharedMaterial = this._movingMat;
	}

	protected override void SetAsDestroyed()
	{
		base.SetAsDestroyed();
		this._renderer.sharedMaterial = this._lockedMat;
	}

	protected override void SetIdle()
	{
		this._renderer.sharedMaterial = (base.ParentDoor.TargetState ? this._openMat : this._closedMat);
	}

	protected override void SetLocked()
	{
		this._renderer.sharedMaterial = this._lockedMat;
	}
}
