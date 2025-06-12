using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace CustomPlayerEffects;

public abstract class StatusEffectBase : MonoBehaviour, IEquatable<StatusEffectBase>
{
	public enum EffectClassification
	{
		Technical,
		Negative,
		Mixed,
		Positive
	}

	private byte _intensity;

	private float _duration;

	private float _timeLeft;

	public byte Intensity
	{
		get
		{
			return this._intensity;
		}
		set
		{
			if (value <= this._intensity || this.AllowEnabling)
			{
				this.ForceIntensity(value);
			}
		}
	}

	public virtual byte MaxIntensity { get; } = byte.MaxValue;

	public bool IsEnabled
	{
		get
		{
			return this.Intensity > 0;
		}
		set
		{
			if (value != this.IsEnabled)
			{
				this.Intensity = (byte)(value ? 1 : 0);
			}
		}
	}

	public virtual bool AllowEnabling
	{
		get
		{
			if (this.Classification != EffectClassification.Negative)
			{
				return true;
			}
			if (!SpawnProtected.CheckPlayer(this.Hub))
			{
				return !Vitality.CheckPlayer(this.Hub);
			}
			return false;
		}
	}

	public virtual EffectClassification Classification => EffectClassification.Negative;

	public bool IsLocalPlayer => this.Hub.isLocalPlayer;

	public bool IsSpectated => this.Hub.IsLocallySpectated();

	public bool IsPOV => this.Hub.IsPOV;

	public float Duration
	{
		get
		{
			return this._duration;
		}
		private set
		{
			this._duration = Mathf.Max(0f, value);
		}
	}

	public float TimeLeft
	{
		get
		{
			return this._timeLeft;
		}
		set
		{
			this._timeLeft = Mathf.Max(0f, value);
			if (this._timeLeft == 0f && this.Duration != 0f)
			{
				this.DisableEffect();
			}
		}
	}

	public ReferenceHub Hub { get; private set; }

	public static event Action<StatusEffectBase> OnEnabled;

	public static event Action<StatusEffectBase> OnDisabled;

	public static event Action<StatusEffectBase, byte, byte> OnIntensityChanged;

	[Server]
	public void ServerSetState(byte intensity, float duration = 0f, bool addDuration = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CustomPlayerEffects.StatusEffectBase::ServerSetState(System.Byte,System.Single,System.Boolean)' called when server was not active");
			return;
		}
		this.Intensity = intensity;
		this.ServerChangeDuration(duration, addDuration);
	}

	[Server]
	public void ServerDisable()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CustomPlayerEffects.StatusEffectBase::ServerDisable()' called when server was not active");
		}
		else
		{
			this.DisableEffect();
		}
	}

	[Server]
	public void ServerChangeDuration(float duration, bool addDuration = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void CustomPlayerEffects.StatusEffectBase::ServerChangeDuration(System.Single,System.Boolean)' called when server was not active");
		}
		else if (addDuration && duration > 0f)
		{
			this.Duration += duration;
			this.TimeLeft += duration;
		}
		else
		{
			this.Duration = duration;
			this.TimeLeft = this.Duration;
		}
	}

	public void ForceIntensity(byte value)
	{
		if (this._intensity == value)
		{
			return;
		}
		byte intensity = this._intensity;
		bool active = NetworkServer.active;
		bool flag = intensity == 0 && value > 0;
		if (active)
		{
			PlayerEffectUpdatingEventArgs e = new PlayerEffectUpdatingEventArgs(this.Hub, this, value, this.Duration);
			PlayerEvents.OnUpdatingEffect(e);
			if (!e.IsAllowed)
			{
				return;
			}
			value = e.Intensity;
			this.Duration = e.Duration;
		}
		this._intensity = (byte)Mathf.Min(value, this.MaxIntensity);
		if (active)
		{
			this.Hub.playerEffectsController.ServerSyncEffect(this);
			PlayerEvents.OnUpdatedEffect(new PlayerEffectUpdatedEventArgs(this.Hub, this, value, this.Duration));
		}
		if (flag)
		{
			this.Enabled();
			StatusEffectBase.OnEnabled?.Invoke(this);
		}
		else if (intensity > 0 && value == 0)
		{
			this.Disabled();
			StatusEffectBase.OnDisabled?.Invoke(this);
		}
		this.IntensityChanged(intensity, value);
		StatusEffectBase.OnIntensityChanged?.Invoke(this, intensity, value);
	}

	protected virtual void Awake()
	{
		this.Hub = ReferenceHub.GetHub(base.transform.root.gameObject);
	}

	protected virtual void Start()
	{
	}

	protected virtual void Update()
	{
		if (this.IsEnabled)
		{
			this.RefreshTime();
			this.OnEffectUpdate();
		}
	}

	private void RefreshTime()
	{
		if (this.Duration != 0f)
		{
			this.TimeLeft -= Time.deltaTime;
		}
	}

	protected virtual void Enabled()
	{
	}

	protected virtual void Disabled()
	{
	}

	protected virtual void OnEffectUpdate()
	{
	}

	protected virtual void IntensityChanged(byte prevState, byte newState)
	{
	}

	public virtual void OnBeginSpectating()
	{
	}

	public virtual void OnStopSpectating()
	{
	}

	internal virtual void OnRoleChanged(PlayerRoleBase previousRole, PlayerRoleBase newRole)
	{
		this.DisableEffect();
	}

	internal virtual void OnDeath(PlayerRoleBase previousRole)
	{
		this.DisableEffect();
	}

	protected virtual void DisableEffect()
	{
		if (NetworkServer.active)
		{
			this.Intensity = 0;
		}
	}

	public bool Equals(StatusEffectBase other)
	{
		if (other != null)
		{
			return other.gameObject == base.gameObject;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != base.GetType())
		{
			return false;
		}
		return this.Equals((StatusEffectBase)obj);
	}

	public override int GetHashCode()
	{
		return base.gameObject.GetHashCode();
	}
}
