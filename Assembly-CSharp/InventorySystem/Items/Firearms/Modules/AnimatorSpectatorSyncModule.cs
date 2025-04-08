using System;
using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.Items.Autosync;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AnimatorSpectatorSyncModule : ModuleBase, ISpectatorSyncModule
	{
		public void SetupViewmodel(AnimatedFirearmViewmodel viewmodel, float defaultSkipTime)
		{
			AnimatorSpectatorSyncModule.SyncAnimData syncAnimData;
			if (!AnimatorSpectatorSyncModule.ReceivedData.TryGetValue(viewmodel.ItemId.SerialNumber, out syncAnimData))
			{
				viewmodel.AnimatorForceUpdate(defaultSkipTime, false);
				return;
			}
			for (int i = 0; i < syncAnimData.Snapshots.Length; i++)
			{
				int num;
				if (this._layers.TryGet(i, out num))
				{
					AnimatorSpectatorSyncModule.AnimLayerSnapshot animLayerSnapshot = syncAnimData.Snapshots[i];
					viewmodel.AnimatorPlay(animLayerSnapshot.TagHash, num, animLayerSnapshot.NormalizedTime);
				}
			}
			float num2 = (float)syncAnimData.LastReceived.Elapsed.TotalSeconds;
			float num3 = Mathf.Min(defaultSkipTime, num2);
			viewmodel.AnimatorForceUpdate(num3, false);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			AnimatorSpectatorSyncModule.SyncAnimData orAdd = AnimatorSpectatorSyncModule.ReceivedData.GetOrAdd(serial, () => new AnimatorSpectatorSyncModule.SyncAnimData(this._layers.Length));
			for (int i = 0; i < this._layers.Length; i++)
			{
				AnimatorSpectatorSyncModule.AnimLayerSnapshot animLayerSnapshot = orAdd.Snapshots[i];
				animLayerSnapshot.TagHash = reader.ReadInt();
				animLayerSnapshot.NormalizedTime = (float)reader.ReadByte() * 0.003921569f;
			}
			orAdd.LastReceived.Restart();
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			if (this._enqueued || !NetworkServer.active)
			{
				return;
			}
			this.ServerUpdateInstance();
			AnimatorSpectatorSyncModule.UpdateQueue.Enqueue(this);
			this._enqueued = true;
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (!NetworkServer.active)
			{
				return;
			}
			this._lastSnapshots = new AnimatorSpectatorSyncModule.AnimLayerSnapshot[this._layers.Length];
			for (int i = 0; i < this._layers.Length; i++)
			{
				this._lastSnapshots[i] = new AnimatorSpectatorSyncModule.AnimLayerSnapshot();
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
				int num = this._layers[i];
				AnimatorStateInfo animatorStateInfo = (serverSideAnimator.IsInTransition(num) ? serverSideAnimator.GetNextAnimatorStateInfo(num) : serverSideAnimator.GetCurrentAnimatorStateInfo(num));
				AnimatorSpectatorSyncModule.AnimLayerSnapshot animLayerSnapshot = this._lastSnapshots[i];
				animLayerSnapshot.NormalizedTime = animatorStateInfo.normalizedTime;
				animLayerSnapshot.TagHash = animatorStateInfo.shortNameHash;
				animLayerSnapshot.Loop = animatorStateInfo.loop;
			}
			this.SendRpc((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole, new Action<NetworkWriter>(this.ServerWriteAnimator));
		}

		private void ServerWriteAnimator(NetworkWriter writer)
		{
			for (int i = 0; i < this._layers.Length; i++)
			{
				AnimatorSpectatorSyncModule.AnimLayerSnapshot animLayerSnapshot = this._lastSnapshots[i];
				float num = animLayerSnapshot.NormalizedTime;
				if (animLayerSnapshot.Loop)
				{
					num -= (float)((int)num);
				}
				else
				{
					num = Mathf.Clamp01(num);
				}
				writer.WriteInt(animLayerSnapshot.TagHash);
				writer.WriteByte((byte)Mathf.RoundToInt(num * 255f));
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void InitOnLoad()
		{
			StaticUnityMethods.OnUpdate += AnimatorSpectatorSyncModule.UpdateInstances;
			PlayerRoleManager.OnServerRoleSet += AnimatorSpectatorSyncModule.OnServerRoleSet;
		}

		private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (newRole.IsAlive())
			{
				return;
			}
			foreach (AutosyncItem autosyncItem in AutosyncItem.Instances)
			{
				if (autosyncItem.IsEquipped)
				{
					Firearm firearm = autosyncItem as Firearm;
					AnimatorSpectatorSyncModule animatorSpectatorSyncModule;
					if (firearm != null && firearm.TryGetModule(out animatorSpectatorSyncModule, true))
					{
						animatorSpectatorSyncModule.SendRpc(userHub, new Action<NetworkWriter>(animatorSpectatorSyncModule.ServerWriteAnimator));
					}
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

		private const int MaxUpdatesPerFrame = 3;

		private static readonly Dictionary<ushort, AnimatorSpectatorSyncModule.SyncAnimData> ReceivedData = new Dictionary<ushort, AnimatorSpectatorSyncModule.SyncAnimData>();

		private static readonly Queue<AnimatorSpectatorSyncModule> UpdateQueue = new Queue<AnimatorSpectatorSyncModule>();

		private bool _enqueued;

		private AnimatorSpectatorSyncModule.AnimLayerSnapshot[] _lastSnapshots;

		[HideInInspector]
		[SerializeField]
		private int[] _layers;

		private class SyncAnimData
		{
			public SyncAnimData(int layers)
			{
				this.LastReceived = new Stopwatch();
				this.Snapshots = new AnimatorSpectatorSyncModule.AnimLayerSnapshot[layers];
				for (int i = 0; i < layers; i++)
				{
					this.Snapshots[i] = new AnimatorSpectatorSyncModule.AnimLayerSnapshot();
				}
			}

			public Stopwatch LastReceived;

			public AnimatorSpectatorSyncModule.AnimLayerSnapshot[] Snapshots;
		}

		private class AnimLayerSnapshot
		{
			public int TagHash;

			public float NormalizedTime;

			public bool Loop;
		}
	}
}
