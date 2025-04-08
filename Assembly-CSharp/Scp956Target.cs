using System;
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
	public bool IsAffected
	{
		get
		{
			float num;
			if (base.Intensity == 255)
			{
				num = 20f;
			}
			else
			{
				num = 2.5f;
			}
			float num2 = num;
			return base.IsEnabled && (double)this._activeTime > (double)num2 - NetworkTime.rtt;
		}
	}

	public Vector3 Position
	{
		get
		{
			return (base.Hub.roleManager.CurrentRole as HumanRole).FpcModule.Position;
		}
	}

	public bool MovementModifierActive
	{
		get
		{
			return this.IsAffected;
		}
	}

	public float MovementSpeedMultiplier
	{
		get
		{
			return 0f;
		}
	}

	public float MovementSpeedLimit
	{
		get
		{
			return 0f;
		}
	}

	public CursorOverrideMode CursorOverride
	{
		get
		{
			return CursorOverrideMode.NoOverride;
		}
	}

	public bool LockMovement
	{
		get
		{
			return base.Hub.isLocalPlayer && this.IsAffected;
		}
	}

	public BlockedInteraction BlockedInteractions
	{
		get
		{
			return BlockedInteraction.All;
		}
	}

	public bool CanBeCleared
	{
		get
		{
			return !base.IsEnabled;
		}
	}

	public HolidayType[] TargetHolidays { get; } = new HolidayType[]
	{
		HolidayType.Christmas,
		HolidayType.AprilFools
	};

	protected override void Start()
	{
		base.Start();
		if (!base.Hub.isLocalPlayer)
		{
			return;
		}
		CursorManager.Register(this);
	}

	protected override void Enabled()
	{
		base.Enabled();
		this._musicSource.Stop();
		this._musicSource.volume = 1f;
		this._musicSource.loop = false;
		Scp956Target.EffectReason intensity = (Scp956Target.EffectReason)base.Intensity;
		if (intensity != Scp956Target.EffectReason.Child)
		{
			if (intensity == Scp956Target.EffectReason.HasCandy)
			{
				this._musicSource.PlayOneShot(this._longClip);
			}
		}
		else
		{
			this._musicSource.PlayOneShot(this._shortClip);
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
		if (!base.IsEnabled)
		{
			this._activeTime = 0f;
			this._musicSource.volume -= Time.deltaTime * 3f;
			return;
		}
		this._activeTime += Time.deltaTime;
		if (!this.IsAffected)
		{
			return;
		}
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
		if (this._musicSource.loop)
		{
			return;
		}
		this._musicSource.loop = true;
		this._musicSource.volume = 1f;
		this._musicSource.Stop();
		this._musicSource.Play();
	}

	private void LookAtPinata()
	{
		Vector3 position = MainCameraController.CurrentCamera.position;
		Vector3 eulerAngles = Quaternion.LookRotation(Scp956Pinata.LastPosition - position, Vector3.up).eulerAngles;
		IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
		if (fpcRole == null)
		{
			return;
		}
		FpcMouseLook mouseLook = fpcRole.FpcModule.MouseLook;
		mouseLook.CurrentHorizontal = Mathf.LerpAngle(mouseLook.CurrentHorizontal, eulerAngles.y, Time.deltaTime * 4f);
		mouseLook.CurrentVertical = Mathf.LerpAngle(mouseLook.CurrentVertical, -eulerAngles.x, Time.deltaTime * 4f);
	}

	private float _activeTime;

	[SerializeField]
	private AudioSource _musicSource;

	[SerializeField]
	private AudioClip _shortClip;

	[SerializeField]
	private AudioClip _longClip;

	public enum EffectReason
	{
		None,
		Child = 254,
		HasCandy
	}
}
