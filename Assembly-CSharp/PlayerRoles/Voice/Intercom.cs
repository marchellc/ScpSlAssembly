using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;
using Utils.NonAllocLINQ;
using VoiceChat;

namespace PlayerRoles.Voice;

public class Intercom : NetworkBehaviour
{
	private static Intercom _singleton;

	private static bool _singletonSet;

	private readonly Stopwatch _sustain = new Stopwatch();

	private readonly Stopwatch _clipSw = Stopwatch.StartNew();

	private readonly HashSet<ReferenceHub> _adminOverrides = new HashSet<ReferenceHub>();

	private ReferenceHub _curSpeaker;

	private float _cooldownTime;

	private float _speechTime;

	private float _rangeSqr;

	private Vector3 _worldPos;

	[SerializeField]
	private float _range;

	[SerializeField]
	private float _wakeupTime;

	[SerializeField]
	private float _sustainTime;

	[SerializeField]
	private AudioClip _startClip;

	[SerializeField]
	private AudioClip _endClip;

	[SerializeField]
	private AudioSource _clipSource;

	[SerializeField]
	private float _clipCooldown;

	[SyncVar]
	private byte _state;

	[SyncVar]
	private double _nextTime;

	private Func<ReferenceHub, bool> _checkPlayer;

	public static IntercomState State
	{
		get
		{
			if (Intercom._singletonSet)
			{
				return (IntercomState)Intercom._singleton._state;
			}
			return IntercomState.NotFound;
		}
		set
		{
			if (!Intercom._singletonSet || !NetworkServer.active)
			{
				return;
			}
			Intercom singleton = Intercom._singleton;
			switch (value)
			{
			case IntercomState.InUse:
				singleton.Network_nextTime = NetworkTime.time + (double)singleton._speechTime;
				break;
			case IntercomState.Starting:
				singleton.Network_nextTime = NetworkTime.time + (double)singleton._wakeupTime;
				Intercom.OnServerBeginUsage?.Invoke(singleton._curSpeaker);
				singleton.RpcPlayClip(state: true);
				break;
			case IntercomState.Cooldown:
				singleton.RpcPlayClip(state: false);
				if (singleton._curSpeaker != null && singleton._curSpeaker.serverRoles.BypassMode)
				{
					singleton.Network_nextTime = 0.0;
				}
				else
				{
					singleton.Network_nextTime = NetworkTime.time + (double)singleton._cooldownTime;
				}
				break;
			}
			singleton.Network_state = (byte)value;
		}
	}

	public float RemainingTime => Mathf.Max((float)(this._nextTime - NetworkTime.time), 0f);

	public bool BypassMode
	{
		get
		{
			if (Intercom.State == IntercomState.InUse)
			{
				return this._nextTime == 0.0;
			}
			return false;
		}
	}

	public byte Network_state
	{
		get
		{
			return this._state;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._state, 1uL, null);
		}
	}

	public double Network_nextTime
	{
		get
		{
			return this._nextTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._nextTime, 2uL, null);
		}
	}

	public static event Action<ReferenceHub> OnServerBeginUsage;

	private void Start()
	{
		if (Intercom._singletonSet)
		{
			throw new InvalidOperationException("Multiple instances of Intercom detected. Last name: '" + base.name + "'");
		}
		Intercom._singleton = this;
		Intercom._singletonSet = true;
		this._checkPlayer = CheckPlayer;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(ReloadConfigs));
		SeedSynchronizer.OnGenerationFinished += SetupPos;
		this.ReloadConfigs();
		if (SeedSynchronizer.MapGenerated)
		{
			this.SetupPos();
		}
	}

	private void Update()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		switch (Intercom.State)
		{
		case IntercomState.Ready:
		{
			if (ReferenceHub.AllHubs.TryGetFirst(this._checkPlayer, out var first))
			{
				this._curSpeaker = first;
				Intercom.State = IntercomState.Starting;
			}
			break;
		}
		case IntercomState.Starting:
			if (!(this._nextTime > NetworkTime.time))
			{
				this._sustain.Restart();
				Intercom.State = IntercomState.InUse;
			}
			break;
		case IntercomState.InUse:
		{
			bool flag;
			if (this._curSpeaker != null && this.CheckPlayer(this._curSpeaker))
			{
				flag = true;
				this._sustain.Restart();
			}
			else
			{
				flag = this._sustain.Elapsed.TotalSeconds < (double)this._sustainTime;
			}
			if (!flag || (!(this._nextTime > NetworkTime.time) && this._nextTime != 0.0))
			{
				Intercom.State = IntercomState.Cooldown;
				PlayerEvents.OnUsedIntercom(new PlayerUsedIntercomEventArgs(this._curSpeaker, Intercom.State));
			}
			break;
		}
		case IntercomState.Cooldown:
			if (!(this._nextTime > NetworkTime.time))
			{
				Intercom.State = IntercomState.Ready;
			}
			break;
		}
	}

	private void OnDestroy()
	{
		ConfigFile.OnConfigReloaded = (Action)Delegate.Remove(ConfigFile.OnConfigReloaded, new Action(ReloadConfigs));
		SeedSynchronizer.OnGenerationFinished -= SetupPos;
		Intercom._singletonSet = false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.position, this._range);
	}

	private void SetupPos()
	{
		this._worldPos = base.transform.position;
		this._rangeSqr = this._range * this._range;
	}

	private void ReloadConfigs()
	{
		this._cooldownTime = ConfigFile.ServerConfig.GetFloat("intercom_cooldown", 120f);
		this._speechTime = ConfigFile.ServerConfig.GetFloat("intercom_max_speech_time", 20f);
	}

	private bool CheckRange(ReferenceHub hub)
	{
		if (hub.roleManager.CurrentRole is HumanRole humanRole)
		{
			return (humanRole.FpcModule.Position - this._worldPos).sqrMagnitude < this._rangeSqr;
		}
		return false;
	}

	private bool CheckPlayer(ReferenceHub hub)
	{
		PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
		if (!this.CheckRange(hub) || !(currentRole as HumanRole).VoiceModule.ServerIsSending || VoiceChatMutes.IsMuted(hub, checkIntercom: true))
		{
			return false;
		}
		PlayerUsingIntercomEventArgs e = new PlayerUsingIntercomEventArgs(hub, Intercom.State);
		PlayerEvents.OnUsingIntercom(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		return true;
	}

	[ClientRpc]
	private void RpcPlayClip(bool state)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBool(state);
		this.SendRPCInternal("System.Void PlayerRoles.Voice.Intercom::RpcPlayClip(System.Boolean)", -1505159510, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public static bool CheckPerms(ReferenceHub hub)
	{
		if (!Intercom._singletonSet)
		{
			return false;
		}
		if (VoiceChatMutes.IsMuted(hub, checkIntercom: true))
		{
			return false;
		}
		bool flag = Intercom.State == IntercomState.InUse;
		if (!Intercom.HasOverride(hub))
		{
			if (flag && Intercom._singleton.CheckRange(hub))
			{
				return Intercom._singleton._curSpeaker == hub;
			}
			return false;
		}
		return true;
	}

	public static bool HasOverride(ReferenceHub hub)
	{
		return Intercom._singleton._adminOverrides.Contains(hub);
	}

	public static bool TrySetOverride(ReferenceHub ply, bool newState)
	{
		if (!Intercom._singletonSet || ply == null)
		{
			return false;
		}
		HashSet<ReferenceHub> adminOverrides = Intercom._singleton._adminOverrides;
		if (!newState)
		{
			return adminOverrides.Remove(ply);
		}
		adminOverrides.Add(ply);
		return true;
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPlayClip__Boolean(bool state)
	{
		if (!(this._clipSw.Elapsed.TotalSeconds < (double)this._clipCooldown))
		{
			this._clipSource.PlayOneShot(state ? this._startClip : this._endClip);
			this._clipSw.Restart();
		}
	}

	protected static void InvokeUserCode_RpcPlayClip__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcPlayClip called on server.");
		}
		else
		{
			((Intercom)obj).UserCode_RpcPlayClip__Boolean(reader.ReadBool());
		}
	}

	static Intercom()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Intercom), "System.Void PlayerRoles.Voice.Intercom::RpcPlayClip(System.Boolean)", InvokeUserCode_RpcPlayClip__Boolean);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this._state);
			writer.WriteDouble(this._nextTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._state);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteDouble(this._nextTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._state, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._nextTime, null, reader.ReadDouble());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._state, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._nextTime, null, reader.ReadDouble());
		}
	}
}
