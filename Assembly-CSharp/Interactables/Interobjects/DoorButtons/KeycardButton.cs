using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class KeycardButton : BasicDoorButton
{
	[SerializeField]
	private float _deniedDuration;

	[SerializeField]
	private GameObject _destroyedRoot;

	[SerializeField]
	private GameObject _nonDestroyedRoot;

	[field: SerializeField]
	protected KeycardScannerNfcIcon NfcScannerIcon { get; private set; }

	[field: SerializeField]
	protected KeycardScannerPermsIndicator PermsIndicator { get; private set; }

	public override void Init(DoorVariant door)
	{
		base.Init(door);
		this.PermsIndicator.Register(door);
	}

	protected override void SetMoving()
	{
		this.NfcScannerIcon.SetGranted();
		this.PermsIndicator.PlayAccepted(null);
	}

	protected override void SetAsDestroyed()
	{
		base.SetAsDestroyed();
		this._destroyedRoot.SetActive(value: true);
		this._nonDestroyedRoot.SetActive(value: false);
		this.NfcScannerIcon.SetError();
	}

	protected override void RestoreNonDestroyed()
	{
		base.RestoreNonDestroyed();
		this._destroyedRoot.SetActive(value: false);
		this._nonDestroyedRoot.SetActive(value: true);
	}

	protected override void SetIdle()
	{
		this.NfcScannerIcon.SetRegular();
		this.PermsIndicator.ShowIdle();
	}

	protected override void SetLocked()
	{
		this.NfcScannerIcon.SetError();
		this.PermsIndicator.ShowIdle();
	}

	protected override void OnDenied(DoorPermissionFlags flags)
	{
		base.OnDenied(flags);
		this.NfcScannerIcon.SetTemporaryDenied(this._deniedDuration);
		this.PermsIndicator.PlayDenied(flags, this._deniedDuration);
	}
}
