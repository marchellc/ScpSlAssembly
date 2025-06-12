using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.Items.Autosync;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class AnimatorSpectatorSyncModule : ModuleBase, ISpectatorSyncModule
{
	private class SyncAnimData
	{
		public Stopwatch LastReceived;

		public AnimLayerSnapshot[] Snapshots;

		public SyncAnimData(int layers)
		{
			this.LastReceived = new Stopwatch();
			this.Snapshots = new AnimLayerSnapshot[layers];
			for (int i = 0; i < layers; i++)
			{
				this.Snapshots[i] = new AnimLayerSnapshot();
			}
		}
	}

	private class AnimLayerSnapshot
	{
		public int TagHash;

		public float NormalizedTime;

		public bool Loop;
	}

	private const int MaxUpdatesPerFrame = 3;

	private static readonly Dictionary<ushort, SyncAnimData> ReceivedData = new Dictionary<ushort, SyncAnimData>();

	private static readonly Queue<AnimatorSpectatorSyncModule> UpdateQueue = new Queue<AnimatorSpectatorSyncModule>();

	private bool _enqueued;

	private AnimLayerSnapshot[] _lastSnapshots;

	[HideInInspector]
	[SerializeField]
	private int[] _layers;

	public void SetupViewmodel(AnimatedFirearmViewmodel viewmodel, float defaultSkipTime)
	{
		if (!AnimatorSpectatorSyncModule.ReceivedData.TryGetValue(viewmodel.ItemId.SerialNumber, out var value))
		{
			viewmodel.AnimatorForceUpdate(defaultSkipTime, fastMode: false);
			return;
		}
		for (int i = 0; i < value.Snapshots.Length; i++)
		{
			if (this._layers.TryGet(i, out var element))
			{
				AnimLayerSnapshot animLayerSnapshot = value.Snapshots[i];
				viewmodel.AnimatorPlay(animLayerSnapshot.TagHash, element, animLayerSnapshot.NormalizedTime);
			}
		}
		float b = (float)value.LastReceived.Elapsed.TotalSeconds;
		float deltaTime = Mathf.Min(defaultSkipTime, b);
		viewmodel.AnimatorForceUpdate(deltaTime, fastMode: false);
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		SyncAnimData orAdd = AnimatorSpectatorSyncModule.ReceivedData.GetOrAdd(serial, () => new SyncAnimData(this._layers.Length));
		for (int num = 0; num < this._layers.Length; num++)
		{
			AnimLayerSnapshot obj = orAdd.Snapshots[num];
			obj.TagHash = reader.ReadInt();
			obj.NormalizedTime = (float)(int)reader.ReadByte() * 0.003921569f;
		}
		orAdd.LastReceived.Restart();
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (!this._enqueued && NetworkServer.active)
		{
			this.ServerUpdateInstance();
			AnimatorSpectatorSyncModule.UpdateQueue.Enqueue(this);
			this._enqueued = true;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		if (NetworkServer.active)
		{
			this._lastSnapshots = new AnimLayerSnapshot[this._layers.Length];
			for (int i = 0; i < this._layers.Length; i++)
			{
				this._lastSnapshots[i] = new AnimLayerSnapshot();
			}
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		AnimatorSpectatorSyncModule.ReceivedData.Clear();
	}

	public override void OnFirearmValidate(Firearm fa)
	{
		base.OnFirearmValidate(fa);
	}

	private void ServerUpdateInstance()
	{
		Animator serverSideAnimator = base.Firearm.ServerSideAnimator;
		for (int i = 0; i < this._layers.Length; i++)
		{
			int layerIndex = this._layers[i];
			AnimatorStateInfo animatorStateInfo = (serverSideAnimator.IsInTransition(layerIndex) ? serverSideAnimator.GetNextAnimatorStateInfo(layerIndex) : serverSideAnimator.GetCurrentAnimatorStateInfo(layerIndex));
			AnimLayerSnapshot obj = this._lastSnapshots[i];
			obj.NormalizedTime = animatorStateInfo.normalizedTime;
			obj.TagHash = animatorStateInfo.shortNameHash;
			obj.Loop = animatorStateInfo.loop;
		}
		this.SendRpc((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole, ServerWriteAnimator);
	}

	private void ServerWriteAnimator(NetworkWriter writer)
	{
		for (int i = 0; i < this._layers.Length; i++)
		{
			AnimLayerSnapshot animLayerSnapshot = this._lastSnapshots[i];
			float normalizedTime = animLayerSnapshot.NormalizedTime;
			normalizedTime = ((!animLayerSnapshot.Loop) ? Mathf.Clamp01(normalizedTime) : (normalizedTime - (float)(int)normalizedTime));
			writer.WriteInt(animLayerSnapshot.TagHash);
			writer.WriteByte((byte)Mathf.RoundToInt(normalizedTime * 255f));
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void InitOnLoad()
	{
		StaticUnityMethods.OnUpdate += UpdateInstances;
		PlayerRoleManager.OnServerRoleSet += OnServerRoleSet;
	}

	private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
	{
		if (newRole.IsAlive())
		{
			return;
		}
		foreach (AutosyncItem instance in AutosyncItem.Instances)
		{
			if (instance.IsEquipped && instance is Firearm firearm && firearm.TryGetModule<AnimatorSpectatorSyncModule>(out var module))
			{
				module.SendRpc(userHub, module.ServerWriteAnimator);
			}
		}
	}

	private static void UpdateInstances()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		int num = Mathf.Min(3, AnimatorSpectatorSyncModule.UpdateQueue.Count);
		for (int i = 0; i < num; i++)
		{
			AnimatorSpectatorSyncModule animatorSpectatorSyncModule = AnimatorSpectatorSyncModule.UpdateQueue.Dequeue();
			if (!(animatorSpectatorSyncModule == null))
			{
				if (animatorSpectatorSyncModule.Firearm.IsEquipped)
				{
					animatorSpectatorSyncModule.ServerUpdateInstance();
					AnimatorSpectatorSyncModule.UpdateQueue.Enqueue(animatorSpectatorSyncModule);
				}
				else
				{
					animatorSpectatorSyncModule._enqueued = false;
				}
			}
		}
	}
}
