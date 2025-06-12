using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1344;

public class Scp1344Viewmodel : UsableItemViewmodel
{
	private static readonly FixedSway FixedSway = new FixedSway(null, Vector3.zero, Vector3.zero);

	private static readonly int StatusAnimHash = Animator.StringToHash("CurStatus");

	private static readonly int InspectAnimHash = Animator.StringToHash("Inspect");

	[SerializeField]
	private AudioClip _wearSound;

	[SerializeField]
	private AudioClip _inspectSound;

	[SerializeField]
	private AudioClip _removeBuildUpSound;

	private AudioSource _audioSource;

	private AudioClip _originalClip;

	private float _originalVolume;

	private Scp1344Status _status;

	public override IItemSwayController SwayController
	{
		get
		{
			Scp1344Status status = this._status;
			if (status != Scp1344Status.Inspecting && status != Scp1344Status.Idle)
			{
				return Scp1344Viewmodel.FixedSway;
			}
			return base.SwayController;
		}
	}

	public override void InitAny()
	{
		base.InitAny();
		Scp1344Viewmodel.FixedSway.SetTransform(base.HandsPivot);
		Scp1344NetworkHandler.OnStatusChanged += ClientChangeStatus;
		this.ClientChangeStatus(base.ItemId.SerialNumber, Scp1344NetworkHandler.GetSavedStatus(base.ItemId.SerialNumber));
	}

	public override void OnUsingStarted()
	{
		base.OnUsingStarted();
		this.PlayClip(this._wearSound);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp1344NetworkHandler.OnStatusChanged -= ClientChangeStatus;
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (!base.IsSpectator)
		{
			this.ClientChangeStatus(base.ItemId.SerialNumber, Scp1344NetworkHandler.GetSavedStatus(base.ItemId.SerialNumber));
		}
	}

	private void ClientChangeStatus(ushort serial, Scp1344Status status)
	{
		if (base.ItemId.SerialNumber == serial)
		{
			switch (status)
			{
			case Scp1344Status.Inspecting:
				this.AnimatorSetTrigger(Scp1344Viewmodel.InspectAnimHash);
				this.PlayClip(this._inspectSound, this._originalVolume);
				break;
			case Scp1344Status.Dropping:
				this.AnimatorSetInt(Scp1344Viewmodel.StatusAnimHash, (int)status);
				this.PlayClip(this._removeBuildUpSound);
				break;
			default:
				this.AnimatorSetInt(Scp1344Viewmodel.StatusAnimHash, (int)status);
				break;
			}
			this._status = status;
		}
	}

	private void Awake()
	{
		this._audioSource = base.gameObject.GetComponent<AudioSource>();
		this._originalClip = this._audioSource.clip;
		this._originalVolume = this._audioSource.volume;
	}

	private void OnDisable()
	{
		this._audioSource.clip = this._originalClip;
	}

	private void PlayClip(AudioClip clip, float volume = 1f)
	{
		this._audioSource.clip = clip;
		this._audioSource.volume = volume;
		this._audioSource.Play();
	}
}
