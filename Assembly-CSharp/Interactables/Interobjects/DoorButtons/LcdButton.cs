using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class LcdButton : SimpleButton
{
	[Serializable]
	private struct MovingIndicator
	{
		[SerializeField]
		private RectTransform _mover;

		[SerializeField]
		private RectTransform _openReference;

		[SerializeField]
		private RectTransform _closedReference;

		public readonly void SetStatus(float openPercent)
		{
			this._mover.position = Vector3.Lerp(this._closedReference.position, this._openReference.position, openPercent);
		}
	}

	[SerializeField]
	private MovingIndicator _leftIndicator;

	[SerializeField]
	private MovingIndicator _rightIndicator;

	[SerializeField]
	private GameObject _movingState;

	[SerializeField]
	private GameObject _openState;

	[SerializeField]
	private GameObject _closedState;

	[SerializeField]
	private GameObject _nonBroken;

	[SerializeField]
	private GameObject _broken;

	[SerializeField]
	private float _refreshCooldown;

	private float _remainingCooldown;

	protected override void SetIdle()
	{
		base.SetIdle();
		this._openState.SetActive(base.ParentDoor.TargetState);
		this._closedState.SetActive(!base.ParentDoor.TargetState);
		this._movingState.SetActive(value: false);
	}

	protected override void SetAsDestroyed()
	{
		base.SetAsDestroyed();
		this._broken.SetActive(value: true);
		this._nonBroken.SetActive(value: false);
	}

	protected override void RestoreNonDestroyed()
	{
		base.RestoreNonDestroyed();
		this._broken.SetActive(value: false);
		this._nonBroken.SetActive(value: true);
	}

	protected override void UpdateMoving()
	{
		base.UpdateMoving();
		if (this._remainingCooldown > 0f)
		{
			this._remainingCooldown -= Time.deltaTime;
		}
		else
		{
			this.UpdateIndicators();
		}
	}

	protected override void SetMoving()
	{
		base.SetMoving();
		this._openState.SetActive(value: false);
		this._closedState.SetActive(value: false);
		this._movingState.SetActive(value: true);
		this.UpdateIndicators();
	}

	private void UpdateIndicators()
	{
		float exactState = base.ParentDoor.GetExactState();
		this._leftIndicator.SetStatus(exactState);
		this._rightIndicator.SetStatus(exactState);
		this._remainingCooldown = this._refreshCooldown;
	}
}
