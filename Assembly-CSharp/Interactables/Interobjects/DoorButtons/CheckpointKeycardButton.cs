using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class CheckpointKeycardButton : KeycardButton
{
	[SerializeField]
	private Material _openMaterial;

	[SerializeField]
	private Material _warningMaterial;

	[SerializeField]
	private GameObject _regularScreen;

	[SerializeField]
	private GameObject _passScreen;

	[SerializeField]
	private GameObject _warningScreen;

	private bool _isIdle;

	public override void Init(DoorVariant door)
	{
		base.Init(door);
		if (!(door is CheckpointDoor checkpointDoor))
		{
			Debug.LogError(base.name + " couldn't successfully attach to a CheckpointDoor.", this);
		}
		else
		{
			checkpointDoor.OnSequenceChanged += UpdateSequence;
		}
	}

	private void UpdateSequence()
	{
		if (this._isIdle)
		{
			CheckpointDoor.SequenceState curSequence = (base.ParentDoor as CheckpointDoor).CurSequence;
			this._passScreen.SetActive(curSequence == CheckpointDoor.SequenceState.OpenLoop);
			this._warningScreen.SetActive(curSequence == CheckpointDoor.SequenceState.ClosingWarning);
			if (curSequence == CheckpointDoor.SequenceState.ClosingWarning)
			{
				base.NfcScannerIcon.SetMaterial(this._warningMaterial, null);
			}
		}
	}

	private void RestoreScreen()
	{
		this._isIdle = false;
		this._regularScreen.SetActive(value: true);
		this._passScreen.SetActive(value: false);
		this._warningScreen.SetActive(value: false);
	}

	protected override void OnDenied(DoorPermissionFlags flags)
	{
		base.OnDenied(flags);
		this.RestoreScreen();
	}

	protected override void SetAsDestroyed()
	{
		base.SetAsDestroyed();
		this.RestoreScreen();
	}

	protected override void SetLocked()
	{
		base.SetLocked();
		this.RestoreScreen();
	}

	protected override void SetIdle()
	{
		base.SetIdle();
		if (base.ParentDoor.TargetState)
		{
			this._isIdle = true;
			this._regularScreen.SetActive(value: false);
			base.NfcScannerIcon.SetMaterial(this._openMaterial, null);
			this.UpdateSequence();
		}
	}

	protected override void RestoreNonDestroyed()
	{
		base.RestoreNonDestroyed();
		this.RestoreScreen();
	}

	protected override void SetMoving()
	{
		base.SetMoving();
		if (!base.ParentDoor.TargetState)
		{
			base.NfcScannerIcon.SetRegular();
			base.PermsIndicator.ShowIdle();
		}
		this.RestoreScreen();
	}
}
