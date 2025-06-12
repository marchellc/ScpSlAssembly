using CursorManagement;
using CustomPlayerEffects;
using InventorySystem.Items;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

public class Scp956Target : StatusEffectBase, IMovementSpeedModifier, ICursorOverride, IInteractionBlocker, IHolidayEffect
{
	public enum EffectReason
	{
		None = 0,
		Child = 254,
		HasCandy = 255
	}

	private float _activeTime;

	[SerializeField]
	private AudioSource _musicSource;

	[SerializeField]
	private AudioClip _shortClip;

	[SerializeField]
	private AudioClip _longClip;

	public bool IsAffected
	{
		get
		{
			float num = ((base.Intensity != byte.MaxValue) ? 2.5f : 20f);
			float num2 = num;
			if (base.IsEnabled)
			{
				return (double)this._activeTime > (double)num2 - NetworkTime.rtt;
			}
			return false;
		}
	}

	public Vector3 Position => (base.Hub.roleManager.CurrentRole as HumanRole).FpcModule.Position;

	public bool MovementModifierActive => this.IsAffected;

	public float MovementSpeedMultiplier => 0f;

	public float MovementSpeedLimit => 0f;

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement
	{
		get
		{
			if (base.Hub.isLocalPlayer)
			{
				return this.IsAffected;
			}
			return false;
		}
	}

	public BlockedInteraction BlockedInteractions => BlockedInteraction.All;

	public bool CanBeCleared => !base.IsEnabled;

	public HolidayType[] TargetHolidays { get; } = new HolidayType[2]
	{
		HolidayType.Christmas,
		HolidayType.AprilFools
	};

	public override EffectClassification Classification => EffectClassification.Technical;

	protected override void Start()
	{
		base.Start();
		if (base.Hub.isLocalPlayer)
		{
			CursorManager.Register(this);
		}
	}

	protected override void Enabled()
	{
		base.Enabled();
		this._musicSource.Stop();
		this._musicSource.volume = 1f;
		this._musicSource.loop = false;
		switch ((EffectReason)base.Intensity)
		{
		case EffectReason.HasCandy:
			this._musicSource.PlayOneShot(this._longClip);
			break;
		case EffectReason.Child:
			this._musicSource.PlayOneShot(this._shortClip);
			break;
		}
		Scp956Pinata.IsSpawned = true;
	}

	private void OnDestroy()
	{
		CursorManager.Unregister(this);
	}

	protected override void Update()
	{
		base.Update();
		this._musicSource.mute = !base.IsLocalPlayer && !base.IsSpectated;
		if (base.IsEnabled)
		{
			this._activeTime += Time.deltaTime;
			if (this.IsAffected)
			{
				if (NetworkServer.active || base.IsLocalPlayer)
				{
					base.Hub.interCoordinator.AddBlocker(this);
				}
				if (NetworkServer.active)
				{
					base.Hub.inventory.ServerSelectItem(0);
				}
				if (base.IsLocalPlayer)
				{
					this.LookAtPinata();
				}
				if (!this._musicSource.loop)
				{
					this._musicSource.loop = true;
					this._musicSource.volume = 1f;
					this._musicSource.Stop();
					this._musicSource.Play();
				}
			}
		}
		else
		{
			this._activeTime = 0f;
			this._musicSource.volume -= Time.deltaTime * 3f;
		}
	}

	private void LookAtPinata()
	{
		Vector3 position = MainCameraController.CurrentCamera.position;
		Vector3 eulerAngles = Quaternion.LookRotation(Scp956Pinata.LastPosition - position, Vector3.up).eulerAngles;
		if (base.Hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			FpcMouseLook mouseLook = fpcRole.FpcModule.MouseLook;
			mouseLook.CurrentHorizontal = Mathf.LerpAngle(mouseLook.CurrentHorizontal, eulerAngles.y, Time.deltaTime * 4f);
			mouseLook.CurrentVertical = Mathf.LerpAngle(mouseLook.CurrentVertical, 0f - eulerAngles.x, Time.deltaTime * 4f);
		}
	}
}
