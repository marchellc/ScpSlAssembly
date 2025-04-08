using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp049
{
	public abstract class RagdollIndicatorsBase<T> : StandardSubroutine<T> where T : PlayerRoleBase
	{
		protected virtual void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (!this.ServerCheckNew())
			{
				this.ServerRevalidateOld();
			}
		}

		protected virtual GameObject GenerateIndicator(BasicRagdoll ragdoll)
		{
			return global::UnityEngine.Object.Instantiate<GameObject>(this._indicatorTemplate);
		}

		protected abstract bool ValidateRagdoll(BasicRagdoll ragdoll);

		private bool ServerCheckNew()
		{
			foreach (BasicRagdoll basicRagdoll in RagdollManager.AllRagdolls)
			{
				if (this.ValidateRagdoll(basicRagdoll) && basicRagdoll.Info.ExistenceTime > this._showDelay && this._availableRagdolls.Add(basicRagdoll))
				{
					this.ServerSendRpc(RagdollIndicatorsBase<T>.ListSyncRpcType.Add, basicRagdoll);
					return true;
				}
			}
			return false;
		}

		private void ServerRevalidateOld()
		{
			BasicRagdoll basicRagdoll;
			if (!this._availableRagdolls.TryGetFirst((BasicRagdoll x) => !this.ValidateRagdoll(x), out basicRagdoll))
			{
				return;
			}
			this._availableRagdolls.Remove(basicRagdoll);
			this.ServerSendRpc(RagdollIndicatorsBase<T>.ListSyncRpcType.Remove, basicRagdoll);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			RagdollManager.OnRagdollRemoved += this.ClientRemoveRagdoll;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._availableRagdolls.Clear();
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
			RagdollManager.OnRagdollRemoved -= this.ClientRemoveRagdoll;
			this._indicatorInstances.ForEachValue(delegate(RagdollIndicatorsBase<T>.Indicator x)
			{
				global::UnityEngine.Object.Destroy(x.Instance);
			});
			this._indicatorInstances.Clear();
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this._rpcType);
			if (this._rpcType != RagdollIndicatorsBase<T>.ListSyncRpcType.FullResync)
			{
				writer.WriteUInt(this._syncRagdoll);
				return;
			}
			this._availableRagdolls.ForEach(delegate(BasicRagdoll x)
			{
				writer.WriteUInt(x.netId);
			});
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._rpcType = (RagdollIndicatorsBase<T>.ListSyncRpcType)reader.ReadByte();
			RagdollIndicatorsBase<T>.ListSyncRpcType rpcType = this._rpcType;
			if (rpcType != RagdollIndicatorsBase<T>.ListSyncRpcType.FullResync)
			{
				if (rpcType - RagdollIndicatorsBase<T>.ListSyncRpcType.Add <= 1)
				{
					this.ClientProcessRpcSingularNetId(reader.ReadUInt(), this._rpcType);
					return;
				}
			}
			else
			{
				while (this._availableRagdolls.Count > 0)
				{
					this.ClientRemoveRagdoll(this._availableRagdolls.First<BasicRagdoll>());
				}
				while (reader.Remaining > 0)
				{
					this.ClientProcessRpcSingularNetId(reader.ReadUInt(), RagdollIndicatorsBase<T>.ListSyncRpcType.Add);
				}
			}
		}

		private void ClientProcessRpcSingularNetId(uint netId, RagdollIndicatorsBase<T>.ListSyncRpcType rpcType)
		{
			NetworkIdentity networkIdentity;
			if (!NetworkUtils.SpawnedNetIds.TryGetValue(netId, out networkIdentity))
			{
				return;
			}
			BasicRagdoll basicRagdoll;
			if (!networkIdentity.TryGetComponent<BasicRagdoll>(out basicRagdoll))
			{
				return;
			}
			if (rpcType == RagdollIndicatorsBase<T>.ListSyncRpcType.Add)
			{
				this._availableRagdolls.Add(basicRagdoll);
				return;
			}
			if (rpcType != RagdollIndicatorsBase<T>.ListSyncRpcType.Remove)
			{
				return;
			}
			this.ClientRemoveRagdoll(basicRagdoll);
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active || !(newRole is SpectatorRole))
			{
				return;
			}
			this._rpcType = RagdollIndicatorsBase<T>.ListSyncRpcType.FullResync;
			base.ServerSendRpc(hub);
		}

		private void ServerSendRpc(RagdollIndicatorsBase<T>.ListSyncRpcType rpcType, BasicRagdoll ragdoll)
		{
			this._rpcType = rpcType;
			this._syncRagdoll = ragdoll.netId;
			base.ServerSendRpc((ReferenceHub x) => x == base.Owner || x.roleManager.CurrentRole is SpectatorRole);
		}

		private void ClientRemoveRagdoll(BasicRagdoll ragdoll)
		{
			this._availableRagdolls.Remove(ragdoll);
			RagdollIndicatorsBase<T>.Indicator indicator;
			if (!this._indicatorInstances.TryGetValue(ragdoll.Info.Serial, out indicator))
			{
				return;
			}
			global::UnityEngine.Object.Destroy(indicator.Instance);
			this._indicatorInstances.Remove(ragdoll.Info.Serial);
		}

		[SerializeField]
		private float _showDelay;

		[SerializeField]
		private float _fullOpacityDistance;

		[SerializeField]
		private float _visibleDistance;

		[SerializeField]
		private GameObject _indicatorTemplate;

		[SerializeField]
		private Vector3 _posOffset;

		private readonly Dictionary<ushort, RagdollIndicatorsBase<T>.Indicator> _indicatorInstances = new Dictionary<ushort, RagdollIndicatorsBase<T>.Indicator>();

		private readonly HashSet<BasicRagdoll> _availableRagdolls = new HashSet<BasicRagdoll>();

		private RagdollIndicatorsBase<T>.ListSyncRpcType _rpcType;

		private uint _syncRagdoll;

		private readonly struct Indicator
		{
			public void SetAlpha(float f)
			{
				f = Mathf.Clamp01(f);
				this._group.alpha = f;
			}

			public Indicator(GameObject inst)
			{
				this.Instance = inst;
				this._group = inst.GetComponentInChildren<CanvasGroup>();
				this.SetAlpha(0f);
			}

			public readonly GameObject Instance;

			private readonly CanvasGroup _group;
		}

		private enum ListSyncRpcType
		{
			FullResync,
			Add,
			Remove
		}
	}
}
