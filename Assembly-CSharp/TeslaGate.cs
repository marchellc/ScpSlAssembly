using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Hazards;
using MapGeneration;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

public class TeslaGate : NetworkBehaviour
{
	public Vector3 Position
	{
		get
		{
			if (this._position == Vector3.zero)
			{
				this._position = base.transform.position;
			}
			return this._position;
		}
	}

	public static event TeslaGate.BurstComplete OnBurstComplete;

	public static event Action<TeslaGate> OnAdded;

	public static event Action<TeslaGate> OnRemoved;

	public static event Action<TeslaGate> OnBursted;

	public void ServerSideCode()
	{
		if (this.InProgress)
		{
			return;
		}
		Timing.RunCoroutine(this.ServerSideWaitForAnimation());
		this.RpcPlayAnimation();
	}

	private IEnumerator<float> ServerSideWaitForAnimation()
	{
		this.InProgress = true;
		yield return Timing.WaitForSeconds(this.windupTime);
		if (this.TantrumsToBeDestroyed.Count > 0)
		{
			this.TantrumsToBeDestroyed.ForEach(delegate(TantrumEnvironmentalHazard tantrum)
			{
				if (tantrum != null)
				{
					tantrum.PlaySizzle = true;
					tantrum.ServerDestroy();
				}
			});
			this.TantrumsToBeDestroyed.Clear();
		}
		yield return Timing.WaitForSeconds(this.cooldownTime);
		this.InProgress = false;
		yield break;
	}

	public void ServerSideIdle(bool shouldIdle)
	{
		if (shouldIdle)
		{
			this.RpcDoIdle();
			return;
		}
		this.RpcDoneIdling();
	}

	private void Update()
	{
	}

	private void Start()
	{
		TeslaGate.AllGates.Add(this);
		Action<TeslaGate> onAdded = TeslaGate.OnAdded;
		if (onAdded == null)
		{
			return;
		}
		onAdded(this);
	}

	private void OnDestroy()
	{
		TeslaGate.AllGates.Remove(this);
		Action<TeslaGate> onRemoved = TeslaGate.OnRemoved;
		if (onRemoved == null)
		{
			return;
		}
		onRemoved(this);
	}

	public void ClientSideCode()
	{
		base.transform.localPosition = this.localPosition;
		base.transform.localRotation = Quaternion.Euler(this.localRotation);
		if (this.ledLights != null)
		{
			this.ledLights.SetBool(TeslaGate._animatorShockHash, this.InProgress);
			this.ledLights.SetBool(TeslaGate._animatorIdleHash, this.isIdling);
		}
	}

	[ClientRpc]
	private void RpcDoIdle()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcDoIdle()", 482546151, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	private void RpcDoneIdling()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcDoneIdling()", -243325925, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	private void RpcPlayAnimation()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcPlayAnimation()", 2031582250, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	public void RpcInstantBurst()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcInstantBurst()", -684941977, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private IEnumerator<float> _DoShock()
	{
		Action<TeslaGate> onBursted = TeslaGate.OnBursted;
		if (onBursted != null)
		{
			onBursted(this);
		}
		this.source.Stop();
		foreach (AudioClip audioClip in this.clipsShock)
		{
			if (audioClip != null)
			{
				this.source.PlayOneShot(audioClip);
			}
		}
		foreach (ParticleSystem particleSystem in this.windupParticles)
		{
			if (particleSystem != null)
			{
				particleSystem.Play();
			}
		}
		foreach (ParticleSystem particleSystem2 in this.shockParticles)
		{
			if (particleSystem2 != null)
			{
				particleSystem2.Play();
			}
		}
		ReferenceHub referenceHub;
		while (!ReferenceHub.TryGetLocalHub(out referenceHub))
		{
			yield return float.NegativeInfinity;
		}
		GameObject gameObject = referenceHub.gameObject;
		yield return Timing.WaitForSeconds(0.25f);
		yield return Timing.WaitForSeconds(0.25f);
		float num = 0f;
		foreach (ParticleSystem particleSystem3 in this.smokeParticles)
		{
			if (!(particleSystem3 == null))
			{
				if (particleSystem3.IsAlive() && num <= 0f)
				{
					num = particleSystem3.main.duration;
				}
				particleSystem3.Play();
			}
		}
		if (!this.isIdling)
		{
			foreach (ParticleSystem particleSystem4 in this.windupParticles)
			{
				if (particleSystem4 != null)
				{
					particleSystem4.Stop();
				}
			}
		}
		yield return Timing.WaitForSeconds(num);
		yield break;
	}

	private IEnumerator<float> _PlayAnimation()
	{
		bool is79 = this.next079burst;
		this.next079burst = false;
		if (!is79)
		{
			foreach (AudioClip audioClip in this.clipsWarmup)
			{
				this.source.PlayOneShot(audioClip);
			}
			ParticleSystem[] array2 = this.windupParticles;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Play();
			}
			yield return Timing.WaitForSeconds(this.windupTime);
		}
		Timing.RunCoroutine(this._DoShock());
		yield return Timing.WaitForSeconds(is79 ? 0.5f : this.cooldownTime);
		yield break;
	}

	public bool PlayerInRange(ReferenceHub player)
	{
		ITeslaControllerRole teslaControllerRole = player.roleManager.CurrentRole as ITeslaControllerRole;
		if (teslaControllerRole != null && !teslaControllerRole.CanActivateShock)
		{
			return false;
		}
		IFpcRole fpcRole = player.roleManager.CurrentRole as IFpcRole;
		return fpcRole != null && this.InRange(fpcRole.FpcModule.Position);
	}

	private bool InRange(Vector3 position)
	{
		return Vector3.Distance(this.Position, position) < this.sizeOfTrigger;
	}

	public bool IsInIdleRange(ReferenceHub player)
	{
		if (!player.IsAlive())
		{
			return false;
		}
		ITeslaControllerRole teslaControllerRole = player.roleManager.CurrentRole as ITeslaControllerRole;
		if (teslaControllerRole != null)
		{
			return teslaControllerRole.IsInIdleRange(this);
		}
		IFpcRole fpcRole = player.roleManager.CurrentRole as IFpcRole;
		return fpcRole != null && this.IsInIdleRange(fpcRole.FpcModule.Position);
	}

	public bool IsInIdleRange(Vector3 position)
	{
		return Vector3.Distance(this.Position, position) < this.distanceToIdle;
	}

	private void OnDrawGizmosSelected()
	{
		if (!this.showGizmos)
		{
			return;
		}
		Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
		GameObject[] array = this.killers;
		for (int i = 0; i < array.Length; i++)
		{
			Gizmos.DrawCube(array[i].transform.position + Vector3.up * (this.sizeOfKiller.y / 2f), this.sizeOfKiller);
		}
		Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
		Gizmos.DrawSphere(this.Position, this.sizeOfTrigger);
	}

	static TeslaGate()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcDoIdle()", new RemoteCallDelegate(TeslaGate.InvokeUserCode_RpcDoIdle));
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcDoneIdling()", new RemoteCallDelegate(TeslaGate.InvokeUserCode_RpcDoneIdling));
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcPlayAnimation()", new RemoteCallDelegate(TeslaGate.InvokeUserCode_RpcPlayAnimation));
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcInstantBurst()", new RemoteCallDelegate(TeslaGate.InvokeUserCode_RpcInstantBurst));
	}

	public override bool Weaved()
	{
		return true;
	}

	public float NetworkInactiveTime
	{
		get
		{
			return this.InactiveTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<float>(value, ref this.InactiveTime, 1UL, null);
		}
	}

	protected void UserCode_RpcDoIdle()
	{
		if (!this.isIdling)
		{
			this.isIdling = true;
			this.loopSource.PlayOneShot(this.idleStart);
			this.loopSource.PlayDelayed(this.idleStart.length);
		}
		foreach (ParticleSystem particleSystem in this.windupParticles)
		{
			if (!particleSystem.isPlaying)
			{
				particleSystem.Play();
			}
		}
	}

	protected static void InvokeUserCode_RpcDoIdle(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDoIdle called on server.");
			return;
		}
		((TeslaGate)obj).UserCode_RpcDoIdle();
	}

	protected void UserCode_RpcDoneIdling()
	{
		if (!this.isIdling)
		{
			return;
		}
		this.isIdling = false;
		this.loopSource.Stop();
		this.loopSource.PlayOneShot(this.idleEnd);
		ParticleSystem[] array = this.windupParticles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	protected static void InvokeUserCode_RpcDoneIdling(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDoneIdling called on server.");
			return;
		}
		((TeslaGate)obj).UserCode_RpcDoneIdling();
	}

	protected void UserCode_RpcPlayAnimation()
	{
		Timing.RunCoroutine(this._PlayAnimation(), Segment.FixedUpdate);
	}

	protected static void InvokeUserCode_RpcPlayAnimation(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayAnimation called on server.");
			return;
		}
		((TeslaGate)obj).UserCode_RpcPlayAnimation();
	}

	protected void UserCode_RpcInstantBurst()
	{
		this.next079burst = true;
		Timing.RunCoroutine(this._PlayAnimation(), Segment.FixedUpdate);
	}

	protected static void InvokeUserCode_RpcInstantBurst(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcInstantBurst called on server.");
			return;
		}
		((TeslaGate)obj).UserCode_RpcInstantBurst();
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this.InactiveTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteFloat(this.InactiveTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.InactiveTime, null, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this.InactiveTime, null, reader.ReadFloat());
		}
	}

	public Vector3 localPosition;

	public Vector3 localRotation;

	public GameObject[] killers;

	public LayerMask killerMask;

	public bool InProgress;

	public Animator ledLights;

	public ParticleSystem[] windupParticles;

	public ParticleSystem[] shockParticles;

	public ParticleSystem[] smokeParticles;

	public static readonly HashSet<TeslaGate> AllGates = new HashSet<TeslaGate>();

	public RoomIdentifier Room;

	private Vector3 _position;

	[Header("Parameters")]
	public Vector3 sizeOfKiller;

	public float sizeOfTrigger;

	public float distanceToIdle;

	public float windupTime;

	public float cooldownTime;

	[Header("Idle Loop")]
	public bool isIdling;

	public AudioSource loopSource;

	public AudioClip idleStart;

	public AudioClip idleLoop;

	public AudioClip idleEnd;

	[Header("Audio")]
	public AudioSource source;

	public AudioClip[] clipsWarmup;

	public AudioClip[] clipsShock;

	public bool showGizmos;

	[SyncVar]
	public float InactiveTime;

	public List<TantrumEnvironmentalHazard> TantrumsToBeDestroyed = new List<TantrumEnvironmentalHazard>();

	private bool next079burst;

	private static readonly int _animatorShockHash = Animator.StringToHash("ShockActive");

	private static readonly int _animatorIdleHash = Animator.StringToHash("IdleActive");

	public delegate void BurstComplete(ReferenceHub hub, TeslaGate teslaGate);
}
