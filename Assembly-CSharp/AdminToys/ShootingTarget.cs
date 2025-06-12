using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration.StaticHelpers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerStatsSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AdminToys;

public class ShootingTarget : AdminToyBase, IDestructible, IBlockStaticBatching, IClientInteractable, IInteractable, IServerInteractable
{
	private enum TargetButton
	{
		IncreaseHP,
		DecreaseHP,
		IncreaseResetTime,
		DecreaseResetTime,
		ManualReset,
		Remove,
		GlobalResults
	}

	private float _hp = 10f;

	private int _maxHp = 10;

	private int _autoDestroyTime;

	private float _avg;

	private GameObject _prevHit;

	[SyncVar]
	private bool _syncMode;

	[SerializeField]
	private float _stepSize = 0.12f;

	[SerializeField]
	private string _targetName;

	[SerializeField]
	private AudioSource _source;

	[SerializeField]
	private AudioClip _hitSound;

	[SerializeField]
	private AudioClip _killSound;

	[SerializeField]
	private AudioClip[] _score;

	[SerializeField]
	private GameObject _hitIndicator;

	[SerializeField]
	private Transform _bullsEye;

	[SerializeField]
	private float _bullsEyeRadius;

	[SerializeField]
	private Vector3[] _bullsEyeBounds;

	[SerializeField]
	private Material _prevHitMat;

	[SerializeField]
	private Text _lastHitInfo;

	[SerializeField]
	private Text _syncText;

	[SerializeField]
	private Text _settingsWindow;

	private readonly List<GameObject> _hits = new List<GameObject>();

	public uint NetworkId => base.netId;

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public Vector3 CenterOfMass => this._bullsEye.position;

	public override string CommandName => "Target" + this._targetName;

	public bool Network_syncMode
	{
		get
		{
			return this._syncMode;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncMode, 32uL, null);
		}
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		if (Physics.Raycast(admin.transform.position - admin.transform.forward, Vector3.down, out var hitInfo, 2f))
		{
			base.transform.position = hitInfo.point;
			base.transform.rotation = Quaternion.Euler(Vector3.up * (Mathf.Round((admin.transform.rotation.eulerAngles.y + 90f) / 10f) * 10f));
		}
		base.OnSpawned(admin, arguments);
	}

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHit)
	{
		if (!(handler is AttackerDamageHandler attackerDamageHandler))
		{
			return false;
		}
		ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
		if (hub == null)
		{
			return false;
		}
		PlayerDamagingShootingTargetEventArgs e = new PlayerDamagingShootingTargetEventArgs(hub, this, handler);
		PlayerEvents.OnDamagingShootingTarget(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		float distance = Vector3.Distance(hub.transform.position, this._bullsEye.position);
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (this._syncMode || allHub == hub)
			{
				this.TargetRpcReceiveData(allHub.characterClassManager.connectionToClient, damage, distance, exactHit, handler);
			}
		}
		PlayerEvents.OnDamagedShootingTarget(new PlayerDamagedShootingTargetEventArgs(hub, this, handler));
		return true;
	}

	public void ClientInteract(InteractableCollider collider)
	{
		this.UseButton((TargetButton)collider.ColliderId);
	}

	private void UseButton(TargetButton tb)
	{
		switch (tb)
		{
		case TargetButton.ManualReset:
			this.ClearTarget();
			break;
		case TargetButton.IncreaseHP:
			this._maxHp = Mathf.Clamp(this._maxHp * 2, 1, 256);
			break;
		case TargetButton.DecreaseHP:
			this._maxHp /= 2;
			break;
		case TargetButton.IncreaseResetTime:
			this._autoDestroyTime = Mathf.Min(this._autoDestroyTime + 1, 10);
			break;
		case TargetButton.DecreaseResetTime:
			this._autoDestroyTime = Mathf.Max(this._autoDestroyTime - 1, 0);
			break;
		}
	}

	private void ClearTarget()
	{
		foreach (GameObject hit in this._hits)
		{
			UnityEngine.Object.Destroy(hit);
		}
		this._hits.Clear();
		this._avg = 0f;
		this._hp = this._maxHp;
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!PermissionsHandler.IsPermitted(ply.serverRoles.Permissions, PlayerPermissions.FacilityManagement))
		{
			return;
		}
		PlayerInteractingShootingTargetEventArgs e = new PlayerInteractingShootingTargetEventArgs(ply, this);
		PlayerEvents.OnInteractingShootingTarget(e);
		if (!e.IsAllowed)
		{
			return;
		}
		switch ((TargetButton)colliderId)
		{
		case TargetButton.Remove:
			NetworkServer.Destroy(base.gameObject);
			break;
		case TargetButton.GlobalResults:
			this.Network_syncMode = !this._syncMode;
			break;
		default:
			if (this._syncMode && !ply.isLocalPlayer)
			{
				this.UseButton((TargetButton)colliderId);
				this.RpcSendInfo(this._maxHp, this._autoDestroyTime);
			}
			break;
		}
		PlayerEvents.OnInteractedShootingTarget(new PlayerInteractedShootingTargetEventArgs(ply, this));
	}

	[TargetRpc]
	private void TargetRpcReceiveData(NetworkConnection conn, float damage, float distance, Vector3 pos, DamageHandlerBase handler)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteFloat(damage);
		writer.WriteFloat(distance);
		writer.WriteVector3(pos);
		writer.WriteDamageHandler(handler);
		this.SendTargetRPCInternal(conn, "System.Void AdminToys.ShootingTarget::TargetRpcReceiveData(Mirror.NetworkConnection,System.Single,System.Single,UnityEngine.Vector3,PlayerStatsSystem.DamageHandlerBase)", -668080035, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcSendInfo(int maxHp, int autoReset)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteInt(maxHp);
		writer.WriteInt(autoReset);
		this.SendRPCInternal("System.Void AdminToys.ShootingTarget::RpcSendInfo(System.Int32,System.Int32)", -479456756, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(this._bullsEye.position, this._bullsEyeRadius);
		Gizmos.DrawWireSphere(this._bullsEye.position, this._stepSize);
		Vector3[] bullsEyeBounds = this._bullsEyeBounds;
		for (int i = 0; i < bullsEyeBounds.Length; i++)
		{
			Vector3 vector = bullsEyeBounds[i];
			Gizmos.DrawWireCube(this._bullsEye.TransformPoint(new Vector3(0f, vector.y, vector.x)), new Vector3(0.04f, 1f, 1f) * vector.z);
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase(NetworkConnection conn, float damage, float distance, Vector3 pos, DamageHandlerBase handler)
	{
		float num;
		if (this._bullsEyeBounds.Length == 0)
		{
			num = Vector3.Distance(this._bullsEye.position, pos);
		}
		else
		{
			num = float.PositiveInfinity;
			Vector3[] bullsEyeBounds = this._bullsEyeBounds;
			for (int i = 0; i < bullsEyeBounds.Length; i++)
			{
				Vector3 vector = bullsEyeBounds[i];
				float num2 = Vector3.Distance(new Bounds(this._bullsEye.TransformPoint(new Vector3(0f, vector.y, vector.x)), Vector3.one * vector.z).ClosestPoint(pos), pos);
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		num = Mathf.Max(0f, num - this._bullsEyeRadius);
		int num3 = Mathf.Min(Mathf.CeilToInt(num / this._stepSize), this._score.Length - 1);
		float num4 = 1f - (float)num3 / ((float)this._score.Length - 1f);
		this._avg += num4;
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		gameObject.GetComponent<Collider>().enabled = false;
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = this._hitIndicator.GetComponent<MeshRenderer>().sharedMaterial;
		gameObject.transform.localScale = this._hitIndicator.transform.localScale;
		gameObject.transform.parent = this._hitIndicator.transform.parent;
		gameObject.transform.position = pos;
		this._hp -= damage;
		this._source.Stop();
		this._source.PlayOneShot(this._score[num3]);
		this._source.PlayOneShot((this._hp < 0f) ? this._killSound : this._hitSound);
		if (this._prevHit != null && this._prevHit.TryGetComponent<MeshRenderer>(out var component))
		{
			component.sharedMaterial = this._prevHitMat;
		}
		this._prevHit = gameObject;
		if (this._autoDestroyTime > 0)
		{
			UnityEngine.Object.Destroy(gameObject, this._autoDestroyTime);
		}
		else
		{
			this._hits.Add(gameObject);
		}
	}

	protected static void InvokeUserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetRpcReceiveData called on server.");
		}
		else
		{
			((ShootingTarget)obj).UserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase(null, reader.ReadFloat(), reader.ReadFloat(), reader.ReadVector3(), reader.ReadDamageHandler());
		}
	}

	protected void UserCode_RpcSendInfo__Int32__Int32(int maxHp, int autoReset)
	{
		this._maxHp = maxHp;
		this._autoDestroyTime = autoReset;
		this.ClearTarget();
	}

	protected static void InvokeUserCode_RpcSendInfo__Int32__Int32(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSendInfo called on server.");
		}
		else
		{
			((ShootingTarget)obj).UserCode_RpcSendInfo__Int32__Int32(reader.ReadInt(), reader.ReadInt());
		}
	}

	static ShootingTarget()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(ShootingTarget), "System.Void AdminToys.ShootingTarget::RpcSendInfo(System.Int32,System.Int32)", InvokeUserCode_RpcSendInfo__Int32__Int32);
		RemoteProcedureCalls.RegisterRpc(typeof(ShootingTarget), "System.Void AdminToys.ShootingTarget::TargetRpcReceiveData(Mirror.NetworkConnection,System.Single,System.Single,UnityEngine.Vector3,PlayerStatsSystem.DamageHandlerBase)", InvokeUserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this._syncMode);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteBool(this._syncMode);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncMode, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncMode, null, reader.ReadBool());
		}
	}
}
