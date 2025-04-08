using System;
using System.Collections.Generic;
using Interactables;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace MapGeneration.Spawnables
{
	[RequireComponent(typeof(AudioSource))]
	public class AudioLog : NetworkBehaviour, IServerInteractable, IInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public bool IsPlaying
		{
			get
			{
				return this._audioSource.isPlaying || this._triggerTimestamp + 3.0 > NetworkTime.time;
			}
		}

		public float MaxHearingRange
		{
			get
			{
				return this._audioSource.maxDistance;
			}
		}

		public float ClipDuration
		{
			get
			{
				return (float)this._audioDuration - this._clipSkipSeconds;
			}
		}

		public Vector3 PlayingLocation
		{
			get
			{
				return this._audioTransform.position;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (this.IsPlaying)
			{
				return;
			}
			this.RpcPlayLog(this._clipSkipSeconds);
			this._triggerTimestamp = NetworkTime.time + (double)this.ClipDuration;
		}

		[ClientRpc]
		private void RpcPlayLog(float playTime)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteFloat(playTime);
			this.SendRPCInternal("System.Void MapGeneration.Spawnables.AudioLog::RpcPlayLog(System.Single)", 1831593247, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void ClientPlayAudio(float playTime = 0f)
		{
			if (playTime > 0f)
			{
				this._audioSource.time = playTime;
			}
			this._audioSource.Play();
		}

		private void ServerSyncClip(ReferenceHub hub)
		{
			if (!this.IsPlaying)
			{
				return;
			}
			double num = this._triggerTimestamp - (double)this.ClipDuration;
			float num2 = (float)(NetworkTime.time - num);
			if (num2 >= this.ClipDuration)
			{
				return;
			}
			this.RpcPlayLog(num2);
		}

		private void Awake()
		{
			this._audioTransform = this._audioSource.transform;
			AudioLog.Instances.Add(this);
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.ServerSyncClip));
		}

		private void OnDestroy()
		{
			AudioLog.Instances.Remove(this);
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.ServerSyncClip));
		}

		static AudioLog()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(AudioLog), "System.Void MapGeneration.Spawnables.AudioLog::RpcPlayLog(System.Single)", new RemoteCallDelegate(AudioLog.InvokeUserCode_RpcPlayLog__Single));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcPlayLog__Single(float playTime)
		{
		}

		protected static void InvokeUserCode_RpcPlayLog__Single(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayLog called on server.");
				return;
			}
			((AudioLog)obj).UserCode_RpcPlayLog__Single(reader.ReadFloat());
		}

		private const double BaseCooldown = 3.0;

		public static readonly List<AudioLog> Instances = new List<AudioLog>();

		[SerializeField]
		[Min(0f)]
		private float _clipSkipSeconds;

		[SerializeField]
		[Min(0f)]
		private double _audioDuration;

		[SerializeField]
		private AudioSource _audioSource;

		private Transform _audioTransform;

		private double _triggerTimestamp;
	}
}
