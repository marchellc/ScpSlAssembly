using System;
using System.Collections.Generic;
using Mirror;
using Mirror.RemoteCalls;
using Security;
using UnityEngine;

public class AmbientSoundPlayer : NetworkBehaviour
{
	private void Start()
	{
		if (!base.isLocalPlayer || !base.isServer)
		{
			return;
		}
		for (int i = 0; i < this.clips.Length; i++)
		{
			this.clips[i].index = i;
		}
		base.Invoke("GenerateRandom", 10f);
	}

	private void GenerateRandom()
	{
		this.list.Clear();
		foreach (AmbientSoundPlayer.AmbientClip ambientClip in this.clips)
		{
			if (!ambientClip.played)
			{
				this.list.Add(ambientClip);
			}
		}
		int num = global::UnityEngine.Random.Range(0, this.list.Count);
		int index = this.list[num].index;
		if (!this.clips[index].repeatable)
		{
			this.clips[index].played = true;
		}
		this.RpcPlaySound(index);
		base.Invoke("GenerateRandom", (float)global::UnityEngine.Random.Range(this.minTime, this.maxTime));
	}

	[ClientRpc]
	private void RpcPlaySound(int id)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteInt(id);
		this.SendRPCInternal("System.Void AmbientSoundPlayer::RpcPlaySound(System.Int32)", -1494438428, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPlaySound__Int32(int id)
	{
	}

	protected static void InvokeUserCode_RpcPlaySound__Int32(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlaySound called on server.");
			return;
		}
		((AmbientSoundPlayer)obj).UserCode_RpcPlaySound__Int32(reader.ReadInt());
	}

	static AmbientSoundPlayer()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AmbientSoundPlayer), "System.Void AmbientSoundPlayer::RpcPlaySound(System.Int32)", new RemoteCallDelegate(AmbientSoundPlayer.InvokeUserCode_RpcPlaySound__Int32));
	}

	public GameObject audioPrefab;

	public int minTime = 30;

	public int maxTime = 60;

	public AmbientSoundPlayer.AmbientClip[] clips;

	private List<AmbientSoundPlayer.AmbientClip> list = new List<AmbientSoundPlayer.AmbientClip>();

	private RateLimit _ambientSoundRateLimit = new RateLimit(4, 3f, null);

	[Serializable]
	public class AmbientClip
	{
		public AudioClip clip;

		public bool repeatable = true;

		public bool is3D = true;

		public bool played;

		public int index;
	}
}
