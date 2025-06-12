using System.Collections.Generic;
using System.Linq;
using Mirror;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp049;

public abstract class RagdollIndicatorsBase<T> : StandardSubroutine<T> where T : PlayerRoleBase
{
	private readonly struct Indicator
	{
		public readonly GameObject Instance;

		private readonly CanvasGroup _group;

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
	}

	private enum ListSyncRpcType
	{
		FullResync,
		Add,
		Remove
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

	private readonly Dictionary<BasicRagdoll, Indicator> _indicatorInstances = new Dictionary<BasicRagdoll, Indicator>();

	private readonly HashSet<BasicRagdoll> _availableRagdolls = new HashSet<BasicRagdoll>();

	private ListSyncRpcType _rpcType;

	private uint _syncRagdoll;

	protected virtual void Update()
	{
		if (NetworkServer.active && !this.ServerCheckNew())
		{
			this.ServerRevalidateOld();
		}
	}

	protected virtual GameObject GenerateIndicator(BasicRagdoll ragdoll)
	{
		return Object.Instantiate(this._indicatorTemplate);
	}

	protected abstract bool ValidateRagdoll(BasicRagdoll ragdoll);

	private bool ServerCheckNew()
	{
		foreach (BasicRagdoll allRagdoll in RagdollManager.AllRagdolls)
		{
			if (this.ValidateRagdoll(allRagdoll) && !(allRagdoll.Info.ExistenceTime <= this._showDelay) && this._availableRagdolls.Add(allRagdoll))
			{
				this.ServerSendRpc(ListSyncRpcType.Add, allRagdoll);
				return true;
			}
		}
		return false;
	}

	private void ServerRevalidateOld()
	{
		if (this._availableRagdolls.TryGetFirst((BasicRagdoll x) => !this.ValidateRagdoll(x), out var first))
		{
			this._availableRagdolls.Remove(first);
			this.ServerSendRpc(ListSyncRpcType.Remove, first);
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		RagdollManager.OnRagdollRemoved += ClientRemoveRagdoll;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._availableRagdolls.Clear();
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
		RagdollManager.OnRagdollRemoved -= ClientRemoveRagdoll;
		this._indicatorInstances.ForEachValue(delegate(Indicator x)
		{
			Object.Destroy(x.Instance);
		});
		this._indicatorInstances.Clear();
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteByte((byte)this._rpcType);
		if (this._rpcType != ListSyncRpcType.FullResync)
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
		this._rpcType = (ListSyncRpcType)reader.ReadByte();
		switch (this._rpcType)
		{
		case ListSyncRpcType.Add:
		case ListSyncRpcType.Remove:
			this.ClientProcessRpcSingularNetId(reader.ReadUInt(), this._rpcType);
			return;
		case ListSyncRpcType.FullResync:
			break;
		default:
			return;
		}
		while (this._availableRagdolls.Count > 0)
		{
			this.ClientRemoveRagdoll(this._availableRagdolls.First());
		}
		while (reader.Remaining > 0)
		{
			this.ClientProcessRpcSingularNetId(reader.ReadUInt(), ListSyncRpcType.Add);
		}
	}

	private void ClientProcessRpcSingularNetId(uint netId, ListSyncRpcType rpcType)
	{
		if (NetworkUtils.SpawnedNetIds.TryGetValue(netId, out var value) && value.TryGetComponent<BasicRagdoll>(out var component))
		{
			switch (rpcType)
			{
			case ListSyncRpcType.Add:
				this._availableRagdolls.Add(component);
				break;
			case ListSyncRpcType.Remove:
				this.ClientRemoveRagdoll(component);
				break;
			}
		}
	}

	private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (NetworkServer.active && newRole is SpectatorRole)
		{
			this._rpcType = ListSyncRpcType.FullResync;
			base.ServerSendRpc(hub);
		}
	}

	private void ServerSendRpc(ListSyncRpcType rpcType, BasicRagdoll ragdoll)
	{
		this._rpcType = rpcType;
		this._syncRagdoll = ragdoll.netId;
		base.ServerSendRpc((ReferenceHub x) => x == base.Owner || x.roleManager.CurrentRole is SpectatorRole);
	}

	private void ClientRemoveRagdoll(BasicRagdoll ragdoll)
	{
		this._availableRagdolls.Remove(ragdoll);
		if (this._indicatorInstances.TryGetValue(ragdoll, out var value))
		{
			Object.Destroy(value.Instance);
			this._indicatorInstances.Remove(ragdoll);
		}
	}
}
