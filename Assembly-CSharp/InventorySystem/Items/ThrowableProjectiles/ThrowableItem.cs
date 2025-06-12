using System;
using System.Diagnostics;
using AudioPooling;
using CustomPlayerEffects;
using Footprinting;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.ThrowableProjectiles;

public class ThrowableItem : ItemBase, IItemDescription, IItemNametag, IItemAlertDrawer, IItemDrawer, IHolidayItem
{
	[Serializable]
	public struct ProjectileSettings
	{
		public float StartVelocity;

		public float UpwardsFactor;

		public float TriggerTime;

		public Vector3 StartTorque;

		public Vector3 RelativePosition;
	}

	public ThrownProjectile Projectile;

	public PhantomProjectile Phantom;

	public ProjectileSettings WeakThrowSettings;

	public ProjectileSettings FullThrowSettings;

	public float ThrowingAnimTime;

	public float CancelAnimTime;

	public AudioClip ThrowClip;

	public AudioClip BeginClip;

	public AudioClip CancelClip;

	public readonly Stopwatch ThrowStopwatch = new Stopwatch();

	public readonly Stopwatch CancelStopwatch = new Stopwatch();

	[SerializeField]
	private float _weight;

	[SerializeField]
	private float _pinPullTime;

	[SerializeField]
	private float _postThrownAnimationTime;

	[SerializeField]
	private bool _repickupable;

	private float _destroyTime;

	private bool _tryFire;

	private bool _alreadyFired;

	private bool _fireWeak;

	private bool _phantomPropelled;

	private bool _messageSent;

	private KeyCode _primaryKey;

	private KeyCode _secondaryKey;

	private PhantomProjectile _phantom;

	private Vector3 _releaseSpeed;

	private Scp1853 _scp1853;

	private const float ServerTimeTolerance = 0.8f;

	private const float MaxTraceTime = 0.1f;

	private const float MaxAheadTime = 0.2f;

	private const float HintBlinkRate = 9f;

	private const float HintBlinkStartTime = 1f;

	private const float HintBlinkTotalTime = 0.7f;

	private const ActionName CancelAction = ActionName.Reload;

	private static readonly Stopwatch TriggerDelay = new Stopwatch();

	public override float Weight => this._weight;

	public override bool AllowHolster
	{
		get
		{
			if (!this.ThrowStopwatch.IsRunning)
			{
				if (this.CancelStopwatch.IsRunning)
				{
					return this.ReadyToCancel;
				}
				return true;
			}
			return false;
		}
	}

	public AlertContent Alert
	{
		get
		{
			if (!this.ReadyToThrow || this._alreadyFired)
			{
				return default(AlertContent);
			}
			float num = (float)this.ThrowStopwatch.Elapsed.TotalSeconds;
			float num2 = 1f + this.ThrowingAnimTime;
			float num3 = num2 + 0.7f;
			bool flag = Mathf.Round(num * 9f) % 2f == 0f;
			if (num < num2 || (num < num3 && flag))
			{
				return default(AlertContent);
			}
			string obj = "$" + new ReadableKeyCode(this.CancelKey).NormalVersion + "$";
			return new AlertContent(TranslationReader.GetFormatted("Facility", 41, "Press {0} to cancel the throw.", obj));
		}
	}

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	[field: SerializeField]
	public HolidayType[] TargetHolidays { get; set; }

	public float ScaledThrowElapsed => (float)(this.ThrowStopwatch.Elapsed.TotalSeconds * (double)this.SpeedMultiplier);

	public float ScaledCancelElapsed => (float)(this.CancelStopwatch.Elapsed.TotalSeconds * (double)this.SpeedMultiplier);

	private float CurrentTimeTolerance
	{
		get
		{
			if (!this.IsLocalPlayer)
			{
				return 0.8f;
			}
			return 1f;
		}
	}

	private bool ReadyToThrow => this.ScaledThrowElapsed >= this.CurrentTimeTolerance * this.ThrowingAnimTime;

	private bool ReadyToCancel => this.ScaledCancelElapsed >= this.CurrentTimeTolerance * this.CancelAnimTime;

	private KeyCode CancelKey => NewInput.GetKey(ActionName.Reload);

	private float SpeedMultiplier
	{
		get
		{
			if (!this._scp1853.IsEnabled)
			{
				return 1f;
			}
			return this._scp1853.ItemUsageSpeedMultiplier;
		}
	}

	public event Action<ThrowableNetworkHandler.RequestType> OnRequestSent;

	public override void OnAdded(ItemPickupBase pickup)
	{
		this._primaryKey = NewInput.GetKey(ActionName.Shoot);
		this._secondaryKey = NewInput.GetKey(ActionName.Zoom);
		this._scp1853 = base.Owner.playerEffectsController.GetEffect<Scp1853>();
	}

	public override void EquipUpdate()
	{
		if (NetworkServer.active)
		{
			this.UpdateServer();
		}
		if (!this.IsLocalPlayer)
		{
			return;
		}
		if (!this.AllowHolster)
		{
			if (this._tryFire)
			{
				this.ClientUpdateTryFire();
			}
			else
			{
				this.ClientUpdateAiming();
			}
		}
		else
		{
			this.ClientUpdateIdle();
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		if (!NetworkServer.active || pickup == null || this._alreadyFired)
		{
			return;
		}
		Vector3 velocity = base.Owner.GetVelocity();
		if (this.ScaledThrowElapsed < this._pinPullTime)
		{
			if (pickup is ThrownProjectile && pickup.TryGetComponent<Rigidbody>(out var component))
			{
				component.linearVelocity = velocity;
			}
		}
		else
		{
			this.ServerThrow(0f, 0f, velocity, Vector3.zero);
			pickup.Info.Locked = true;
			pickup.DestroySelf();
		}
	}

	public override void OnHolstered()
	{
		if (this.IsLocalPlayer && !NetworkServer.active)
		{
			this._tryFire = false;
			this._messageSent = false;
			this._alreadyFired = false;
			this._phantomPropelled = false;
			this.ThrowStopwatch.Reset();
		}
	}

	public void ServerProcessThrowConfirmation(bool fullForce, Vector3 startPos, Quaternion startRot, Vector3 startVel)
	{
		if (this.ReadyToThrow)
		{
			Transform playerCameraReference = base.Owner.PlayerCameraReference;
			Vector3 position = playerCameraReference.position;
			Quaternion rotation = playerCameraReference.rotation;
			Bounds bounds = base.Owner.GenerateTracerBounds(0.1f, ignoreTeleports: false);
			bounds.Encapsulate(playerCameraReference.position + base.Owner.GetVelocity() * 0.2f);
			playerCameraReference.SetPositionAndRotation(bounds.ClosestPoint(startPos), startRot);
			ProjectileSettings projectileSettings = (fullForce ? this.FullThrowSettings : this.WeakThrowSettings);
			startVel = ThrowableNetworkHandler.GetLimitedVelocity(startVel);
			PlayerThrowingProjectileEventArgs e = new PlayerThrowingProjectileEventArgs(base.Owner, this, projectileSettings, fullForce);
			PlayerEvents.OnThrowingProjectile(e);
			if (!e.IsAllowed)
			{
				projectileSettings = e.ProjectileSettings;
				fullForce = e.FullForce;
				new ThrowableNetworkHandler.ThrowableItemRequestMessage(this, ThrowableNetworkHandler.RequestType.ForceCancel).SendToAuthenticated();
				this.CancelStopwatch.Start();
				this.ThrowStopwatch.Reset();
				playerCameraReference.SetPositionAndRotation(position, rotation);
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, base.PickupDropModel);
				base.OwnerInventory.ServerAddItem(base.ItemTypeId, ItemAddReason.PickedUp, base.ItemSerial);
			}
			else
			{
				ServerLogs.AddLog(ServerLogs.Modules.Throwable, $"{base.Owner.LoggedNameFromRefHub()} threw {base.ItemTypeId}.", ServerLogs.ServerLogType.GameEvent);
				ThrownProjectile projectile = this.ServerThrow(projectileSettings.StartVelocity, projectileSettings.UpwardsFactor, projectileSettings.StartTorque, startVel);
				new ThrowableNetworkHandler.ThrowableItemAudioMessage(base.ItemSerial, (!fullForce) ? ThrowableNetworkHandler.RequestType.ConfirmThrowWeak : ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce).SendToAuthenticated();
				playerCameraReference.SetPositionAndRotation(position, rotation);
				PlayerEvents.OnThrewProjectile(new PlayerThrewProjectileEventArgs(base.Owner, this, projectile, projectileSettings, fullForce));
			}
		}
	}

	public void ServerProcessInitiation()
	{
		if (this.AllowHolster && (!base.ItemTypeId.TryGetSpeedMultiplier(base.Owner, out var multiplier) || !(multiplier <= 0f)))
		{
			this.ThrowStopwatch.Start();
			this.CancelStopwatch.Reset();
			new ThrowableNetworkHandler.ThrowableItemAudioMessage(base.ItemSerial, ThrowableNetworkHandler.RequestType.BeginThrow).SendToAuthenticated();
		}
	}

	public void ServerProcessCancellation()
	{
		if (this.ReadyToThrow && !this._alreadyFired && !this.CancelStopwatch.IsRunning && !(this.CancelAnimTime <= 0f))
		{
			this.CancelStopwatch.Start();
			this.ThrowStopwatch.Reset();
			new ThrowableNetworkHandler.ThrowableItemAudioMessage(base.ItemSerial, ThrowableNetworkHandler.RequestType.CancelThrow).SendToAuthenticated();
		}
	}

	public void ClientForceCancel()
	{
		if (this._phantomPropelled && this._phantom != null)
		{
			this._phantom.Replace();
		}
		this.CancelStopwatch.Start();
		this.ThrowStopwatch.Stop();
		this._tryFire = false;
		this._messageSent = false;
		this._alreadyFired = false;
		this._phantomPropelled = false;
	}

	private void ClientUpdatePostFire()
	{
		ProjectileSettings projectileSettings = (this._fireWeak ? this.WeakThrowSettings : this.FullThrowSettings);
		double totalSeconds = ThrowableItem.TriggerDelay.Elapsed.TotalSeconds;
		if (!this._messageSent && totalSeconds > (double)projectileSettings.TriggerTime - NetworkTime.rtt)
		{
			ThrowableNetworkHandler.RequestType type = (this._fireWeak ? ThrowableNetworkHandler.RequestType.ConfirmThrowWeak : ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce);
			this._phantom = UnityEngine.Object.Instantiate(this.Phantom);
			this._phantom.Init(base.ItemSerial);
			NetworkClient.Send(new ThrowableNetworkHandler.ThrowableItemRequestMessage(this, type, this._releaseSpeed));
			this._messageSent = true;
		}
		if (!this._phantomPropelled && totalSeconds > (double)projectileSettings.TriggerTime && this._phantom != null)
		{
			this._phantom.Activate(base.Owner.PlayerCameraReference, projectileSettings.RelativePosition);
			Vector3 velocityVector = this.GetCameraVector(projectileSettings.UpwardsFactor) * projectileSettings.StartVelocity + this._releaseSpeed;
			this.PropelBody(this._phantom.Rigidbody, projectileSettings.StartTorque, velocityVector);
			this._phantomPropelled = true;
		}
	}

	private void ClientUpdateTryFire()
	{
		if (this._alreadyFired)
		{
			this.ClientUpdatePostFire();
			return;
		}
		this._alreadyFired = true;
		ThrowableItem.TriggerDelay.Restart();
		this.PlaySound(this.BeginClip);
		this.OnRequestSent?.Invoke(this._fireWeak ? ThrowableNetworkHandler.RequestType.ConfirmThrowWeak : ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce);
		if (base.Owner.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			this._releaseSpeed = fpcRole.FpcModule.Motor.MoveDirection;
			this._releaseSpeed.y = Mathf.Max(this._releaseSpeed.y, 0f);
			if (this._releaseSpeed.y > 1f)
			{
				this._releaseSpeed.y = fpcRole.FpcModule.JumpSpeed;
			}
			this._releaseSpeed = ThrowableNetworkHandler.GetLimitedVelocity(this._releaseSpeed);
		}
	}

	private void ClientUpdateAiming()
	{
		bool key = Input.GetKey(this._primaryKey);
		bool key2 = Input.GetKey(this._secondaryKey);
		bool flag = key || key2;
		if (this.ReadyToThrow)
		{
			if (this.ClientTryCancel())
			{
				return;
			}
			if (!flag)
			{
				this._tryFire = true;
				return;
			}
		}
		if (flag)
		{
			this._fireWeak = key2 && !key;
		}
	}

	private void ClientUpdateIdle()
	{
		if (InventoryGuiController.ItemsSafeForInteraction && (Input.GetKeyDown(this._primaryKey) || Input.GetKeyDown(this._secondaryKey)) && !base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
		{
			this.ThrowStopwatch.Start();
			this.CancelStopwatch.Reset();
			this.PlaySound(this.BeginClip);
			NetworkClient.Send(new ThrowableNetworkHandler.ThrowableItemRequestMessage(this, ThrowableNetworkHandler.RequestType.BeginThrow));
			this.OnRequestSent?.Invoke(ThrowableNetworkHandler.RequestType.BeginThrow);
		}
	}

	private bool ClientTryCancel()
	{
		if (!Input.GetKey(this.CancelKey))
		{
			return false;
		}
		if (this.CancelAnimTime <= 0f || !this.ReadyToThrow)
		{
			return false;
		}
		this.CancelStopwatch.Start();
		this.ThrowStopwatch.Reset();
		this.PlaySound(this.CancelClip);
		this.OnRequestSent?.Invoke(ThrowableNetworkHandler.RequestType.CancelThrow);
		NetworkClient.Send(new ThrowableNetworkHandler.ThrowableItemRequestMessage(this, ThrowableNetworkHandler.RequestType.CancelThrow));
		return true;
	}

	private void PlaySound(AudioClip clip)
	{
		AudioSourcePoolManager.Play2D(clip, 1f, MixerChannel.DefaultSfx, this.SpeedMultiplier);
	}

	private void UpdateServer()
	{
		if (this._destroyTime != 0f && Time.timeSinceLevelLoad >= this._destroyTime)
		{
			base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
		}
	}

	private Vector3 GetCameraVector(float upwardFactor)
	{
		float num = 1f - Mathf.Abs(Vector3.Dot(base.Owner.PlayerCameraReference.forward, Vector3.up));
		Vector3 forward = base.Owner.PlayerCameraReference.forward;
		Vector3 vector = base.Owner.PlayerCameraReference.up * upwardFactor;
		return forward + vector * num;
	}

	private void PropelBody(Rigidbody rb, Vector3 torque, Vector3 velocityVector)
	{
		rb.centerOfMass = Vector3.zero;
		rb.angularVelocity = torque;
		rb.linearVelocity = velocityVector;
	}

	private ThrownProjectile ServerThrow(float forceAmount, float upwardFactor, Vector3 torque, Vector3 startVel)
	{
		if (this._alreadyFired && !this.IsLocalPlayer)
		{
			return null;
		}
		this._destroyTime = Time.timeSinceLevelLoad + this._postThrownAnimationTime;
		this._alreadyFired = true;
		ThrownProjectile thrownProjectile = UnityEngine.Object.Instantiate(this.Projectile, base.Owner.PlayerCameraReference.position, base.Owner.PlayerCameraReference.rotation);
		PickupSyncInfo networkInfo = new PickupSyncInfo(base.ItemTypeId, this.Weight, base.ItemSerial)
		{
			Locked = !this._repickupable
		};
		thrownProjectile.NetworkInfo = networkInfo;
		thrownProjectile.PreviousOwner = new Footprint(base.Owner);
		NetworkServer.Spawn(thrownProjectile.gameObject);
		Vector3 vector = this.GetCameraVector(upwardFactor) * forceAmount + startVel;
		if (thrownProjectile.TryGetComponent<Rigidbody>(out var component))
		{
			this.PropelBody(component, torque, vector);
		}
		thrownProjectile.ServerOnThrown(torque, vector);
		thrownProjectile.ServerActivate();
		return thrownProjectile;
	}
}
