using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerStatsSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AdminToys
{
	public class ShootingTarget : AdminToyBase, IDestructible, IClientInteractable, IInteractable, IServerInteractable
	{
		public uint NetworkId
		{
			get
			{
				return base.netId;
			}
		}

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public Vector3 CenterOfMass
		{
			get
			{
				return this._bullsEye.position;
			}
		}

		public override string CommandName
		{
			get
			{
				return "Target" + this._targetName;
			}
		}

		public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			RaycastHit raycastHit;
			if (Physics.Raycast(admin.transform.position - admin.transform.forward, Vector3.down, out raycastHit, 2f))
			{
				base.transform.position = raycastHit.point;
				base.transform.rotation = Quaternion.Euler(Vector3.up * (Mathf.Round((admin.transform.rotation.eulerAngles.y + 90f) / 10f) * 10f));
			}
			base.OnSpawned(admin, arguments);
		}

		public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHit)
		{
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return false;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub == null)
			{
				return false;
			}
			PlayerDamagingShootingTargetEventArgs playerDamagingShootingTargetEventArgs = new PlayerDamagingShootingTargetEventArgs(hub, this, handler);
			PlayerEvents.OnDamagingShootingTarget(playerDamagingShootingTargetEventArgs);
			if (!playerDamagingShootingTargetEventArgs.IsAllowed)
			{
				return false;
			}
			float num = Vector3.Distance(hub.transform.position, this._bullsEye.position);
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (this._syncMode || referenceHub == hub)
				{
					this.TargetRpcReceiveData(referenceHub.characterClassManager.connectionToClient, damage, num, exactHit, handler);
				}
			}
			PlayerEvents.OnDamagedShootingTarget(new PlayerDamagedShootingTargetEventArgs(hub, this, handler));
			return true;
		}

		public void ClientInteract(InteractableCollider collider)
		{
			this.UseButton((ShootingTarget.TargetButton)collider.ColliderId);
		}

		private void UseButton(ShootingTarget.TargetButton tb)
		{
			switch (tb)
			{
			case ShootingTarget.TargetButton.IncreaseHP:
				this._maxHp = Mathf.Clamp(this._maxHp * 2, 1, 256);
				return;
			case ShootingTarget.TargetButton.DecreaseHP:
				this._maxHp /= 2;
				return;
			case ShootingTarget.TargetButton.IncreaseResetTime:
				this._autoDestroyTime = Mathf.Min(this._autoDestroyTime + 1, 10);
				return;
			case ShootingTarget.TargetButton.DecreaseResetTime:
				this._autoDestroyTime = Mathf.Max(this._autoDestroyTime - 1, 0);
				return;
			case ShootingTarget.TargetButton.ManualReset:
				this.ClearTarget();
				return;
			default:
				return;
			}
		}

		private void ClearTarget()
		{
			foreach (GameObject gameObject in this._hits)
			{
				global::UnityEngine.Object.Destroy(gameObject);
			}
			this._hits.Clear();
			this._avg = 0f;
			this._hp = (float)this._maxHp;
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (!PermissionsHandler.IsPermitted(ply.serverRoles.Permissions, PlayerPermissions.FacilityManagement))
			{
				return;
			}
			PlayerInteractingShootingTargetEventArgs playerInteractingShootingTargetEventArgs = new PlayerInteractingShootingTargetEventArgs(ply, this);
			PlayerEvents.OnInteractingShootingTarget(playerInteractingShootingTargetEventArgs);
			if (!playerInteractingShootingTargetEventArgs.IsAllowed)
			{
				return;
			}
			if (colliderId != 5)
			{
				if (colliderId != 6)
				{
					if (this._syncMode && !ply.isLocalPlayer)
					{
						this.UseButton((ShootingTarget.TargetButton)colliderId);
						this.RpcSendInfo(this._maxHp, this._autoDestroyTime);
					}
				}
				else
				{
					this.Network_syncMode = !this._syncMode;
				}
			}
			else
			{
				NetworkServer.Destroy(base.gameObject);
			}
			PlayerEvents.OnInteractedShootingTarget(new PlayerInteractedShootingTargetEventArgs(ply, this));
		}

		[TargetRpc]
		private void TargetRpcReceiveData(NetworkConnection conn, float damage, float distance, Vector3 pos, DamageHandlerBase handler)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteFloat(damage);
			networkWriterPooled.WriteFloat(distance);
			networkWriterPooled.WriteVector3(pos);
			networkWriterPooled.WriteDamageHandler(handler);
			this.SendTargetRPCInternal(conn, "System.Void AdminToys.ShootingTarget::TargetRpcReceiveData(Mirror.NetworkConnection,System.Single,System.Single,UnityEngine.Vector3,PlayerStatsSystem.DamageHandlerBase)", -668080035, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[ClientRpc]
		private void RpcSendInfo(int maxHp, int autoReset)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteInt(maxHp);
			networkWriterPooled.WriteInt(autoReset);
			this.SendRPCInternal("System.Void AdminToys.ShootingTarget::RpcSendInfo(System.Int32,System.Int32)", -479456756, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(this._bullsEye.position, this._bullsEyeRadius);
			Gizmos.DrawWireSphere(this._bullsEye.position, this._stepSize);
			foreach (Vector3 vector in this._bullsEyeBounds)
			{
				Gizmos.DrawWireCube(this._bullsEye.TransformPoint(new Vector3(0f, vector.y, vector.x)), new Vector3(0.04f, 1f, 1f) * vector.z);
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool Network_syncMode
		{
			get
			{
				return this._syncMode;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this._syncMode, 32UL, null);
			}
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
				foreach (Vector3 vector in this._bullsEyeBounds)
				{
					Bounds bounds = new Bounds(this._bullsEye.TransformPoint(new Vector3(0f, vector.y, vector.x)), Vector3.one * vector.z);
					float num2 = Vector3.Distance(bounds.ClosestPoint(pos), pos);
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
			MeshRenderer meshRenderer;
			if (this._prevHit != null && this._prevHit.TryGetComponent<MeshRenderer>(out meshRenderer))
			{
				meshRenderer.sharedMaterial = this._prevHitMat;
			}
			this._prevHit = gameObject;
			if (this._autoDestroyTime > 0)
			{
				global::UnityEngine.Object.Destroy(gameObject, (float)this._autoDestroyTime);
				return;
			}
			this._hits.Add(gameObject);
		}

		protected static void InvokeUserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC TargetRpcReceiveData called on server.");
				return;
			}
			((ShootingTarget)obj).UserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase(null, reader.ReadFloat(), reader.ReadFloat(), reader.ReadVector3(), reader.ReadDamageHandler());
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
				return;
			}
			((ShootingTarget)obj).UserCode_RpcSendInfo__Int32__Int32(reader.ReadInt(), reader.ReadInt());
		}

		static ShootingTarget()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(ShootingTarget), "System.Void AdminToys.ShootingTarget::RpcSendInfo(System.Int32,System.Int32)", new RemoteCallDelegate(ShootingTarget.InvokeUserCode_RpcSendInfo__Int32__Int32));
			RemoteProcedureCalls.RegisterRpc(typeof(ShootingTarget), "System.Void AdminToys.ShootingTarget::TargetRpcReceiveData(Mirror.NetworkConnection,System.Single,System.Single,UnityEngine.Vector3,PlayerStatsSystem.DamageHandlerBase)", new RemoteCallDelegate(ShootingTarget.InvokeUserCode_TargetRpcReceiveData__NetworkConnection__Single__Single__Vector3__DamageHandlerBase));
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
			if ((base.syncVarDirtyBits & 32UL) != 0UL)
			{
				writer.WriteBool(this._syncMode);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._syncMode, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 32L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._syncMode, null, reader.ReadBool());
			}
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
	}
}
