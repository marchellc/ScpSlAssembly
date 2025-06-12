using System;
using System.Collections.Generic;
using Mirror;
using Mirror.RemoteCalls;
using Security;
using UnityEngine;

public class AmbientSoundPlayer : NetworkBehaviour
{
	[Serializable]
	public class AmbientClip
	{
		public AudioClip clip;

		public bool repeatable = true;

		public bool is3D = true;

		public bool played;

		public int index;
	}

	public GameObject audioPrefab;

	public int minTime = 30;

	public int maxTime = 60;

	public AmbientClip[] clips;

	private List<AmbientClip> list = new List<AmbientClip>();

	private RateLimit _ambientSoundRateLimit = new RateLimit(4, 3f);

	private void Start()
	{
		if (base.isLocalPlayer && base.isServer)
		{
			for (int i = 0; i < this.clips.Length; i++)
			{
				this.clips[i].index = i;
			}
			base.Invoke("GenerateRandom", 10f);
		}
	}

	private void GenerateRandom()
	{
		this.list.Clear();
		int num = 0;
		AmbientClip[] array = this.clips;
		foreach (AmbientClip ambientClip in array)
		{
			if (!ambientClip.played)
			{
				this.list.Add(ambientClip);
			}
		}
		num = UnityEngine.Random.Range(0, this.list.Count);
		int index = this.list[num].index;
		if (!this.clips[index].repeatable)
		{
			this.clips[index].played = true;
		}
		this.RpcPlaySound(index);
		base.Invoke("GenerateRandom", UnityEngine.Random.Range(this.minTime, this.maxTime));
	}

	[ClientRpc]
	private void RpcPlaySound(int id)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteInt(id);
		this.SendRPCInternal("System.Void AmbientSoundPlayer::RpcPlaySound(System.Int32)", -1494438428, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
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
		}
		else
		{
			((AmbientSoundPlayer)obj).UserCode_RpcPlaySound__Int32(reader.ReadInt());
		}
	}

	static AmbientSoundPlayer()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AmbientSoundPlayer), "System.Void AmbientSoundPlayer::RpcPlaySound(System.Int32)", InvokeUserCode_RpcPlaySound__Int32);
	}
}
