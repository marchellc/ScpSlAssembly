using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace CustomPlayerEffects
{
	public abstract class StatusEffectBase : MonoBehaviour, IEquatable<StatusEffectBase>
	{
		public byte Intensity
		{
			get
			{
				return this._intensity;
			}
			set
			{
				if (value > this._intensity && !this.AllowEnabling)
				{
					return;
				}
				this.ForceIntensity(value);
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
				if (value == this.IsEnabled)
				{
					return;
				}
				this.Intensity = (value ? 1 : 0);
			}
		}

		public virtual bool AllowEnabling
		{
			get
			{
				return this.Classification != StatusEffectBase.EffectClassification.Negative || (!SpawnProtected.CheckPlayer(this.Hub) && !Vitality.CheckPlayer(this.Hub));
			}
		}

		public virtual StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Negative;
			}
		}

		public bool IsLocalPlayer
		{
			get
			{
				return this.Hub.isLocalPlayer;
			}
		}

		public bool IsSpectated
		{
			get
			{
				return this.Hub.IsLocallySpectated();
			}
		}

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
				return;
			}
			this.DisableEffect();
		}

		[Server]
		public void ServerChangeDuration(float duration, bool addDuration = false)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void CustomPlayerEffects.StatusEffectBase::ServerChangeDuration(System.Single,System.Boolean)' called when server was not active");
				return;
			}
			if (addDuration && duration > 0f)
			{
				this.Duration += duration;
				this.TimeLeft += duration;
				return;
			}
			this.Duration = duration;
			this.TimeLeft = this.Duration;
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
				PlayerEffectUpdatingEventArgs playerEffectUpdatingEventArgs = new PlayerEffectUpdatingEventArgs(this.Hub, this, value, this.Duration);
				PlayerEvents.OnUpdatingEffect(playerEffectUpdatingEventArgs);
				if (!playerEffectUpdatingEventArgs.IsAllowed)
				{
					return;
				}
				value = playerEffectUpdatingEventArgs.Intensity;
				this.Duration = playerEffectUpdatingEventArgs.Duration;
			}
			this._intensity = (byte)Mathf.Min((int)value, (int)this.MaxIntensity);
			if (active)
			{
				this.Hub.playerEffectsController.ServerSyncEffect(this);
				PlayerEvents.OnUpdatedEffect(new PlayerEffectUpdatedEventArgs(this.Hub, this, value, this.Duration));
			}
			if (flag)
			{
				this.Enabled();
				Action<StatusEffectBase> onEnabled = StatusEffectBase.OnEnabled;
				if (onEnabled != null)
				{
					onEnabled(this);
				}
			}
			else if (intensity > 0 && value == 0)
			{
				this.Disabled();
				Action<StatusEffectBase> onDisabled = StatusEffectBase.OnDisabled;
				if (onDisabled != null)
				{
					onDisabled(this);
				}
			}
			this.IntensityChanged(intensity, value);
			Action<StatusEffectBase, byte, byte> onIntensityChanged = StatusEffectBase.OnIntensityChanged;
			if (onIntensityChanged == null)
			{
				return;
			}
			onIntensityChanged(this, intensity, value);
		}

		private void Awake()
		{
			this.Hub = ReferenceHub.GetHub(base.transform.root.gameObject);
			this.OnAwake();
		}

		protected virtual void Start()
		{
		}

		protected virtual void Update()
		{
			if (!this.IsEnabled)
			{
				return;
			}
			this.RefreshTime();
			this.OnEffectUpdate();
		}

		private void RefreshTime()
		{
			if (this.Duration == 0f)
			{
				return;
			}
			this.TimeLeft -= Time.deltaTime;
		}

		protected virtual void Enabled()
		{
		}

		protected virtual void Disabled()
		{
		}

		protected virtual void OnAwake()
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

		public static event Action<StatusEffectBase> OnEnabled;

		public static event Action<StatusEffectBase> OnDisabled;

		public static event Action<StatusEffectBase, byte, byte> OnIntensityChanged;

		public bool Equals(StatusEffectBase other)
		{
			return other != null && other.gameObject == base.gameObject;
		}

		public override bool Equals(object obj)
		{
			return obj != null && (this == obj || (!(obj.GetType() != base.GetType()) && this.Equals((StatusEffectBase)obj)));
		}

		public override int GetHashCode()
		{
			return base.gameObject.GetHashCode();
		}

		private byte _intensity;

		private float _duration;

		private float _timeLeft;

		public enum EffectClassification
		{
			Technical,
			Negative,
			Mixed,
			Positive
		}
	}
}
