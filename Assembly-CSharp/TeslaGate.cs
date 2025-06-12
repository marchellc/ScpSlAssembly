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
	public delegate void BurstComplete(ReferenceHub hub, TeslaGate teslaGate);

	public Vector3 localPosition;

	public Vector3 localRotation;

	public GameObject[] killers;

	public LayerMask killerMask;

	public bool InProgress;

	public Animator ledLights;

	public ParticleSystem[] windupParticles;

	public ParticleSystem[] shockParticles;

	public ParticleSystem[] smokeParticles;

	public static readonly HashSet<TeslaGate> AllGates;

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

	private static readonly int _animatorShockHash;

	private static readonly int _animatorIdleHash;

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

	public float NetworkInactiveTime
	{
		get
		{
			return this.InactiveTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.InactiveTime, 1uL, null);
		}
	}

	public static event BurstComplete OnBurstComplete;

	public static event Action<TeslaGate> OnAdded;

	public static event Action<TeslaGate> OnRemoved;

	public static event Action<TeslaGate> OnBursted;

	public void ServerSideCode()
	{
		if (!this.InProgress)
		{
			Timing.RunCoroutine(this.ServerSideWaitForAnimation());
			this.RpcPlayAnimation();
		}
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
	}

	public void ServerSideIdle(bool shouldIdle)
	{
		if (shouldIdle)
		{
			this.RpcDoIdle();
		}
		else
		{
			this.RpcDoneIdling();
		}
	}

	private void Update()
	{
	}

	private void Start()
	{
		TeslaGate.AllGates.Add(this);
		TeslaGate.OnAdded?.Invoke(this);
	}

	private void OnDestroy()
	{
		TeslaGate.AllGates.Remove(this);
		TeslaGate.OnRemoved?.Invoke(this);
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
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcDoIdle()", 482546151, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcDoneIdling()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcDoneIdling()", -243325925, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcPlayAnimation()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcPlayAnimation()", 2031582250, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	public void RpcInstantBurst()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void TeslaGate::RpcInstantBurst()", -684941977, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private IEnumerator<float> _DoShock()
	{
		TeslaGate.OnBursted?.Invoke(this);
		this.source.Stop();
		AudioClip[] array = this.clipsShock;
		foreach (AudioClip audioClip in array)
		{
			if (audioClip != null)
			{
				this.source.PlayOneShot(audioClip);
			}
		}
		ParticleSystem[] array2 = this.windupParticles;
		foreach (ParticleSystem particleSystem in array2)
		{
			if (particleSystem != null)
			{
				particleSystem.Play();
			}
		}
		array2 = this.shockParticles;
		foreach (ParticleSystem particleSystem2 in array2)
		{
			if (particleSystem2 != null)
			{
				particleSystem2.Play();
			}
		}
		ReferenceHub hub;
		while (!ReferenceHub.TryGetLocalHub(out hub))
		{
			yield return float.NegativeInfinity;
		}
		_ = hub.gameObject;
		yield return Timing.WaitForSeconds(0.25f);
		yield return Timing.WaitForSeconds(0.25f);
		float num = 0f;
		array2 = this.smokeParticles;
		foreach (ParticleSystem particleSystem3 in array2)
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
			array2 = this.windupParticles;
			foreach (ParticleSystem particleSystem4 in array2)
			{
				if (particleSystem4 != null)
				{
					particleSystem4.Stop();
				}
			}
		}
		yield return Timing.WaitForSeconds(num);
	}

	private IEnumerator<float> _PlayAnimation()
	{
		bool is079 = this.next079burst;
		this.next079burst = false;
		if (!is079)
		{
			AudioClip[] array = this.clipsWarmup;
			foreach (AudioClip clip in array)
			{
				this.source.PlayOneShot(clip);
			}
			ParticleSystem[] array2 = this.windupParticles;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Play();
			}
			yield return Timing.WaitForSeconds(this.windupTime);
		}
		Timing.RunCoroutine(this._DoShock());
		yield return Timing.WaitForSeconds(is079 ? 0.5f : this.cooldownTime);
	}

	public bool PlayerInRange(ReferenceHub player)
	{
		if (player.roleManager.CurrentRole is ITeslaControllerRole { CanActivateShock: false })
		{
			return false;
		}
		if (!(player.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		return this.InRange(fpcRole.FpcModule.Position);
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
		if (player.roleManager.CurrentRole is ITeslaControllerRole teslaControllerRole)
		{
			return teslaControllerRole.IsInIdleRange(this);
		}
		if (!(player.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		return this.IsInIdleRange(fpcRole.FpcModule.Position);
	}

	public bool IsInIdleRange(Vector3 position)
	{
		return Vector3.Distance(this.Position, position) < this.distanceToIdle;
	}

	private void OnDrawGizmosSelected()
	{
		if (this.showGizmos)
		{
			Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
			GameObject[] array = this.killers;
			for (int i = 0; i < array.Length; i++)
			{
				Gizmos.DrawCube(array[i].transform.position + Vector3.up * (this.sizeOfKiller.y / 2f), this.sizeOfKiller);
			}
			Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
			Gizmos.DrawSphere(this.Position, this.sizeOfTrigger);
		}
	}

	static TeslaGate()
	{
		TeslaGate.AllGates = new HashSet<TeslaGate>();
		TeslaGate._animatorShockHash = Animator.StringToHash("ShockActive");
		TeslaGate._animatorIdleHash = Animator.StringToHash("IdleActive");
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcDoIdle()", InvokeUserCode_RpcDoIdle);
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcDoneIdling()", InvokeUserCode_RpcDoneIdling);
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcPlayAnimation()", InvokeUserCode_RpcPlayAnimation);
		RemoteProcedureCalls.RegisterRpc(typeof(TeslaGate), "System.Void TeslaGate::RpcInstantBurst()", InvokeUserCode_RpcInstantBurst);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcDoIdle()
	{
		if (!this.isIdling)
		{
			this.isIdling = true;
			this.loopSource.PlayOneShot(this.idleStart);
			this.loopSource.PlayDelayed(this.idleStart.length);
		}
		ParticleSystem[] array = this.windupParticles;
		foreach (ParticleSystem particleSystem in array)
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
		}
		else
		{
			((TeslaGate)obj).UserCode_RpcDoIdle();
		}
	}

	protected void UserCode_RpcDoneIdling()
	{
		if (this.isIdling)
		{
			this.isIdling = false;
			this.loopSource.Stop();
			this.loopSource.PlayOneShot(this.idleEnd);
			ParticleSystem[] array = this.windupParticles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Stop();
			}
		}
	}

	protected static void InvokeUserCode_RpcDoneIdling(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDoneIdling called on server.");
		}
		else
		{
			((TeslaGate)obj).UserCode_RpcDoneIdling();
		}
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
		}
		else
		{
			((TeslaGate)obj).UserCode_RpcPlayAnimation();
		}
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
		}
		else
		{
			((TeslaGate)obj).UserCode_RpcInstantBurst();
		}
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
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(this.InactiveTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.InactiveTime, null, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.InactiveTime, null, reader.ReadFloat());
		}
	}
}
