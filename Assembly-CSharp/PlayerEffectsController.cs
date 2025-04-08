using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Items;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;
using UnityEngine.Audio;
using Utils.NonAllocLINQ;

public class PlayerEffectsController : NetworkBehaviour
{
	public StatusEffectBase[] AllEffects { get; private set; }

	public int EffectsLength { get; private set; }

	public bool TryGetEffect(string effectName, out StatusEffectBase playerEffect)
	{
		foreach (StatusEffectBase statusEffectBase in this.AllEffects)
		{
			if (statusEffectBase.ToString().StartsWith(effectName, StringComparison.InvariantCultureIgnoreCase))
			{
				playerEffect = statusEffectBase;
				return true;
			}
		}
		playerEffect = null;
		return false;
	}

	public bool TryGetEffect<T>(out T playerEffect) where T : StatusEffectBase
	{
		StatusEffectBase statusEffectBase;
		if (this._effectsByType.TryGetValue(typeof(T), out statusEffectBase))
		{
			T t = statusEffectBase as T;
			if (t != null)
			{
				playerEffect = t;
				return true;
			}
		}
		playerEffect = default(T);
		return false;
	}

	[Server]
	public void UseMedicalItem(ItemBase item)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerEffectsController::UseMedicalItem(InventorySystem.Items.ItemBase)' called when server was not active");
			return;
		}
		foreach (StatusEffectBase statusEffectBase in this.AllEffects)
		{
			if (statusEffectBase.IsEnabled)
			{
				IHealableEffect healableEffect = statusEffectBase as IHealableEffect;
				if (healableEffect != null && healableEffect.IsHealable(item.ItemTypeId))
				{
					ICustomHealableEffect customHealableEffect = statusEffectBase as ICustomHealableEffect;
					if (customHealableEffect != null)
					{
						customHealableEffect.OnHeal(item.ItemTypeId);
					}
					else
					{
						statusEffectBase.IsEnabled = false;
					}
				}
			}
		}
	}

	[Server]
	public StatusEffectBase ChangeState(string effectName, byte intensity, float duration = 0f, bool addDuration = false)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'CustomPlayerEffects.StatusEffectBase PlayerEffectsController::ChangeState(System.String,System.Byte,System.Single,System.Boolean)' called when server was not active");
			return null;
		}
		StatusEffectBase statusEffectBase;
		if (this.TryGetEffect(effectName, out statusEffectBase))
		{
			statusEffectBase.ServerSetState(intensity, duration, addDuration);
		}
		return statusEffectBase;
	}

	[Server]
	public T ChangeState<T>(byte intensity, float duration = 0f, bool addDuration = false) where T : StatusEffectBase
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'T PlayerEffectsController::ChangeState(System.Byte,System.Single,System.Boolean)' called when server was not active");
			return default(T);
		}
		T t;
		if (this.TryGetEffect<T>(out t))
		{
			t.ServerSetState(intensity, duration, addDuration);
		}
		return t;
	}

	[Server]
	public T EnableEffect<T>(float duration = 0f, bool addDuration = false) where T : StatusEffectBase
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'T PlayerEffectsController::EnableEffect(System.Single,System.Boolean)' called when server was not active");
			return default(T);
		}
		return this.ChangeState<T>(1, duration, addDuration);
	}

	[Server]
	public T DisableEffect<T>() where T : StatusEffectBase
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'T PlayerEffectsController::DisableEffect()' called when server was not active");
			return default(T);
		}
		return this.ChangeState<T>(0, 0f, false);
	}

	public void DisableAllEffects()
	{
		StatusEffectBase[] allEffects = this.AllEffects;
		for (int i = 0; i < allEffects.Length; i++)
		{
			allEffects[i].ServerDisable();
		}
	}

	public T GetEffect<T>() where T : StatusEffectBase
	{
		T t;
		if (!this.TryGetEffect<T>(out t))
		{
			return default(T);
		}
		return t;
	}

	[Server]
	public void ServerSyncEffect(StatusEffectBase effect)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerEffectsController::ServerSyncEffect(CustomPlayerEffects.StatusEffectBase)' called when server was not active");
			return;
		}
		for (int i = 0; i < this.EffectsLength; i++)
		{
			StatusEffectBase statusEffectBase = this.AllEffects[i];
			if (statusEffectBase == effect)
			{
				this._syncEffectsIntensity[i] = statusEffectBase.Intensity;
				return;
			}
		}
	}

	public void ServerSendPulse<T>() where T : IPulseEffect
	{
		for (int i = 0; i < this.EffectsLength; i++)
		{
			if (this.AllEffects[i] is T)
			{
				byte index = (byte)Mathf.Min(i, 255);
				this.TargetRpcReceivePulse(this._hub.connectionToClient, index);
				SpectatorNetworking.ForeachSpectatorOf(this._hub, delegate(ReferenceHub x)
				{
					this.TargetRpcReceivePulse(x.connectionToClient, index);
				});
				return;
			}
		}
	}

	[TargetRpc]
	private void TargetRpcReceivePulse(NetworkConnection _, byte effectIndex)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteByte(effectIndex);
		this.SendTargetRPCInternal(_, "System.Void PlayerEffectsController::TargetRpcReceivePulse(Mirror.NetworkConnection,System.Byte)", 483637978, networkWriterPooled, 0);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private void Awake()
	{
		this._hub = ReferenceHub.GetHub(base.gameObject);
		List<StatusEffectBase> list = ListPool<StatusEffectBase>.Shared.Rent();
		foreach (StatusEffectBase statusEffectBase in this.effectsGameObject.GetComponentsInChildren<StatusEffectBase>())
		{
			IHolidayEffect holidayEffect = statusEffectBase as IHolidayEffect;
			if (holidayEffect != null && !holidayEffect.IsAvailable)
			{
				statusEffectBase.gameObject.SetActive(false);
			}
			else
			{
				list.Add(statusEffectBase);
			}
		}
		this.AllEffects = list.ToArray();
		this.EffectsLength = this.AllEffects.Length;
		ListPool<StatusEffectBase>.Shared.Return(list);
		foreach (StatusEffectBase statusEffectBase2 in this.AllEffects)
		{
			this._effectsByType.Add(statusEffectBase2.GetType(), statusEffectBase2);
			this._syncEffectsIntensity.Add(0);
		}
	}

	private void Update()
	{
	}

	private void Start()
	{
		this.effectsGameObject.SetActive(true);
	}

	private void OnEnable()
	{
		PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
	}

	private void OnDisable()
	{
		PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
	}

	private void OnRoleChanged(ReferenceHub targetHub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (targetHub != this._hub)
		{
			return;
		}
		bool flag = oldRole.Team != Team.Dead && newRole.Team == Team.Dead;
		foreach (StatusEffectBase statusEffectBase in this.AllEffects)
		{
			if (flag)
			{
				statusEffectBase.OnDeath(oldRole);
			}
			else
			{
				statusEffectBase.OnRoleChanged(oldRole, newRole);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SpectatorTargetTracker.OnTargetChanged += delegate
		{
			ReferenceHub referenceHub;
			StatusEffectBase[] array;
			if (ReferenceHub.AllHubs.TryGetFirst((ReferenceHub x) => x.playerEffectsController._wasSpectated, out referenceHub))
			{
				array = referenceHub.playerEffectsController.AllEffects;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].OnStopSpectating();
				}
				referenceHub.playerEffectsController._wasSpectated = false;
			}
			ReferenceHub referenceHub2;
			if (!SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub2))
			{
				return;
			}
			PlayerEffectsController playerEffectsController = referenceHub2.playerEffectsController;
			array = playerEffectsController.AllEffects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnBeginSpectating();
			}
			playerEffectsController._wasSpectated = true;
		};
	}

	public PlayerEffectsController()
	{
		base.InitSyncObject(this._syncEffectsIntensity);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetRpcReceivePulse__NetworkConnection__Byte(NetworkConnection _, byte effectIndex)
	{
		int num = Mathf.Min((int)effectIndex, this.EffectsLength - 1);
		IPulseEffect pulseEffect = this.AllEffects[num] as IPulseEffect;
		if (pulseEffect != null)
		{
			pulseEffect.ExecutePulse();
		}
	}

	protected static void InvokeUserCode_TargetRpcReceivePulse__NetworkConnection__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRpcReceivePulse called on server.");
			return;
		}
		((PlayerEffectsController)obj).UserCode_TargetRpcReceivePulse__NetworkConnection__Byte(null, reader.ReadByte());
	}

	static PlayerEffectsController()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(PlayerEffectsController), "System.Void PlayerEffectsController::TargetRpcReceivePulse(Mirror.NetworkConnection,System.Byte)", new RemoteCallDelegate(PlayerEffectsController.InvokeUserCode_TargetRpcReceivePulse__NetworkConnection__Byte));
	}

	public AudioMixer mixer;

	public GameObject effectsGameObject;

	private readonly Dictionary<Type, StatusEffectBase> _effectsByType = new Dictionary<Type, StatusEffectBase>();

	private readonly SyncList<byte> _syncEffectsIntensity = new SyncList<byte>();

	private bool _wasSpectated;

	private ReferenceHub _hub;
}
