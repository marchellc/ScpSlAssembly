using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items;
using MapGeneration;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.NonAllocLINQ;

public class Scp956Pinata : NetworkBehaviour
{
	public static bool TryGetInstance(out Scp956Pinata instance)
	{
		instance = Scp956Pinata._instance;
		return instance != null;
	}

	public static bool IsSpawned
	{
		get
		{
			return Scp956Pinata._instance._spawned;
		}
		set
		{
			Scp956Pinata._instance.Network_spawned = value;
		}
	}

	private static float RandomSign
	{
		get
		{
			return (float)((global::UnityEngine.Random.value > 0.5f) ? 1 : (-1));
		}
	}

	[ClientRpc]
	private void RpcAttack()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Scp956Pinata::RpcAttack()", 977535511, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private void Awake()
	{
		this._tr = base.transform;
		Scp956Pinata._instance = this;
	}

	private void Update()
	{
		this.UpdateVisual();
		if (NetworkServer.active)
		{
			this.UpdateAi();
		}
		this.UpdatePos();
	}

	private void UpdateVisual()
	{
		this._attackTimer += Time.deltaTime;
		this._attackMover.localPosition = Vector3.forward * this._attackCurve.Evaluate(this._attackTimer);
		float num = Mathf.Clamp01(this._fade + this._appearSpeed * Time.deltaTime * (float)(this._spawned ? 1 : (-1)));
		if (num == this._fade)
		{
			return;
		}
		this._fade = num;
		this._appearMat.SetFloat(Scp559Cake.ShaderDissolveProperty, 1f - this._fade);
		bool flag = this._carpincho == 69;
		if (this._fade > 0f)
		{
			if (flag)
			{
				this._capybara.SetActive(true);
			}
			else
			{
				this._visuals.SetActive(true);
			}
		}
		this._tr.rotation = Quaternion.Euler(Vector3.up * this._syncRot);
	}

	private void UpdateAi()
	{
		this._respawnTimer -= Time.deltaTime;
		if (!this._spawned && !Scp956Pinata._hasBeenGiftSpawned)
		{
			if (this._respawnTimer > 0f)
			{
				return;
			}
			Vector3 vector;
			if (!this.TryGetSpawnPos(out vector))
			{
				this._respawnTimer = this._changePositionCooldown;
				return;
			}
			this.Network_carpincho = (byte)global::UnityEngine.Random.Range(0, 255);
			this.Network_syncPos = vector;
			this._spawnPos = vector;
			this.Network_syncRot = global::UnityEngine.Random.value * 360f;
			this.Network_spawned = true;
			this.Network_flying = false;
			this._respawnTimer = this._huntingTime;
			return;
		}
		else
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Scp956Target.EffectReason effectReason = this.CheckSpawnCondition(referenceHub);
				if (effectReason == Scp956Target.EffectReason.None || !this.CheckPlayer(referenceHub))
				{
					if (Scp956Pinata.ActiveTargets.Remove(referenceHub))
					{
						referenceHub.playerEffectsController.DisableEffect<Scp956Target>();
					}
				}
				else
				{
					Scp956Pinata.ActiveTargets.Add(referenceHub);
					referenceHub.playerEffectsController.GetEffect<Scp956Target>().Intensity = (byte)effectReason;
					this._respawnTimer = this._huntingTime;
				}
			}
			if (this._respawnTimer < 0f)
			{
				this.TryDespawn();
				return;
			}
			bool flag = this._foundTarget == null;
			if (!flag && (this.CheckSpawnCondition(this._foundTarget.Hub) == Scp956Target.EffectReason.None || !Scp956Pinata.ActiveTargets.Contains(this._foundTarget.Hub)))
			{
				this._foundTarget = null;
				flag = true;
			}
			if (flag)
			{
				float num = float.MaxValue;
				bool flag2 = false;
				foreach (ReferenceHub referenceHub2 in Scp956Pinata.ActiveTargets)
				{
					Scp956Target effect = referenceHub2.playerEffectsController.GetEffect<Scp956Target>();
					if (effect.IsAffected)
					{
						float sqrMagnitude = (effect.Position - this._tr.position).sqrMagnitude;
						if (sqrMagnitude <= num)
						{
							this._foundTarget = effect;
							num = sqrMagnitude;
							flag2 = true;
						}
					}
				}
				if (!flag2)
				{
					return;
				}
				this._sequenceTimer = 0f;
				this._initialRot = this._syncRot;
				this._initialPos = this._syncPos;
				this._attackTriggered = false;
			}
			this._sequenceTimer += Time.deltaTime;
			Vector3 normalized = (this._foundTarget.Position - this._tr.position).normalized;
			float num2 = Vector3.Angle(normalized, Vector3.forward) * Mathf.Sign(Vector3.Dot(normalized, Vector3.right));
			this.Network_syncRot = Mathf.LerpAngle(this._initialRot, num2, (this._sequenceTimer - 1f) / 4f);
			if (this._sequenceTimer < 6f)
			{
				return;
			}
			Vector3 vector2 = this._foundTarget.Position - this._tr.forward.NormalizeIgnoreY();
			if (Mathf.Abs(this._spawnPos.y - this._foundTarget.Position.y) < this._heightTolerance)
			{
				vector2.y = this._spawnPos.y;
				this.Network_flying = false;
			}
			else
			{
				vector2.y = this._foundTarget.Position.y;
				this.Network_flying = true;
			}
			this.Network_syncPos = Vector3.Lerp(this._initialPos, vector2, (this._sequenceTimer - 6f) / 7f);
			if (this._sequenceTimer < 13.3f)
			{
				return;
			}
			if (!this._attackTriggered)
			{
				this.RpcAttack();
				this._attackTriggered = true;
			}
			if (this._sequenceTimer < 13.5f)
			{
				return;
			}
			this._foundTarget.Hub.playerStats.DealDamage(new Scp956DamageHandler(normalized));
			this._foundTarget = null;
			return;
		}
	}

	public void SpawnBehindTarget(ReferenceHub target)
	{
		Scp956Pinata.ActiveTargets.Add(target);
		this._foundTarget = target.playerEffectsController.EnableEffect<Scp956Target>(0f, false);
		this._foundTarget.Intensity = 254;
		this._respawnTimer = this._huntingTime;
		Transform transform = target.transform;
		this._initialPos = (this.Network_syncPos = (this._spawnPos = transform.position - transform.forward * 2f));
		this._initialRot = (this.Network_syncRot = global::UnityEngine.Random.value * 360f);
		this._respawnTimer = this._huntingTime;
		this._attackTriggered = false;
		Scp956Pinata._hasBeenGiftSpawned = (this.Network_spawned = true);
	}

	private void TryDespawn()
	{
		Vector3 thisPos = this._tr.position;
		if (ReferenceHub.AllHubs.Any(delegate(ReferenceHub x)
		{
			IFpcRole fpcRole = x.roleManager.CurrentRole as IFpcRole;
			return fpcRole != null && this.CheckVisibility(thisPos, fpcRole.FpcModule.Position);
		}))
		{
			return;
		}
		this.Network_spawned = false;
		this._respawnTimer = this._changePositionCooldown;
	}

	private bool CheckPlayer(ReferenceHub hub)
	{
		Vector3 position = this._tr.position;
		Vector3 position2 = (hub.roleManager.CurrentRole as HumanRole).FpcModule.Position;
		float num = this._targettingRange * this._targettingRange;
		WaypointBase waypointBase;
		return (position - position2).sqrMagnitude <= num && this.CheckVisibility(position, position2) && (!WaypointBase.TryGetWaypoint(new RelativePosition(position2).WaypointId, out waypointBase) || !(waypointBase is ElevatorWaypoint));
	}

	private bool TryGetSpawnPos(out Vector3 pos)
	{
		pos = default(Vector3);
		Scp956Pinata.ZonesByIntensity.Clear();
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			if (this.CheckSpawnCondition(referenceHub) == Scp956Target.EffectReason.Child)
			{
				HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
				if (humanRole != null)
				{
					RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(humanRole.FpcModule.Position, true);
					if (!(roomIdentifier == null) && this._spawnableZones.Contains(roomIdentifier.Zone))
					{
						int num;
						Scp956Pinata.ZonesByIntensity.TryGetValue(roomIdentifier.Zone, out num);
						Scp956Pinata.ZonesByIntensity[roomIdentifier.Zone] = num + 1;
					}
				}
			}
		}
		if (Scp956Pinata.ZonesByIntensity.Count == 0)
		{
			return false;
		}
		FacilityZone facilityZone = FacilityZone.None;
		int num2 = 0;
		foreach (KeyValuePair<FacilityZone, int> keyValuePair in Scp956Pinata.ZonesByIntensity)
		{
			if (keyValuePair.Value >= num2)
			{
				facilityZone = keyValuePair.Key;
				num2 = keyValuePair.Value;
			}
		}
		Scp956Pinata.CompatibleDoors.Clear();
		using (HashSet<DoorVariant>.Enumerator enumerator3 = DoorVariant.AllDoors.GetEnumerator())
		{
			while (enumerator3.MoveNext())
			{
				DoorVariant doorVariant = enumerator3.Current;
				if (doorVariant is BreakableDoor && doorVariant.Rooms != null && doorVariant.Rooms.Length != 0 && doorVariant.Rooms[0].Zone == facilityZone)
				{
					Scp956Pinata.CompatibleDoors.Add(doorVariant);
				}
			}
			goto IL_02D1;
		}
		IL_0196:
		int num3 = global::UnityEngine.Random.Range(0, Scp956Pinata.CompatibleDoors.Count);
		Transform transform = Scp956Pinata.CompatibleDoors[num3].transform;
		Scp956Pinata.CompatibleDoors.RemoveAt(num3);
		pos = transform.position + this._doorOffset * Scp956Pinata.RandomSign * transform.forward + Vector3.up * this._raycastHeight;
		RaycastHit raycastHit;
		if (Physics.SphereCast(pos, this._pinataRadius, transform.right * Scp956Pinata.RandomSign, out raycastHit, this._raycastRange, FpcStateProcessor.Mask) && raycastHit.distance >= this._minDistance)
		{
			Vector3 vector = raycastHit.point + raycastHit.normal * this._pinataRadius;
			pos = new Vector3(vector.x, transform.position.y + this._floorHeight, vector.z);
			Vector3 checkPos = pos;
			if (!ReferenceHub.AllHubs.Any(delegate(ReferenceHub x)
			{
				IFpcRole fpcRole = x.roleManager.CurrentRole as IFpcRole;
				return fpcRole != null && this.CheckVisibility(checkPos, fpcRole.FpcModule.Position);
			}))
			{
				return true;
			}
		}
		IL_02D1:
		if (Scp956Pinata.CompatibleDoors.Count <= 0)
		{
			return false;
		}
		goto IL_0196;
	}

	private bool CheckVisibility(Vector3 checkPos, Vector3 humanPos)
	{
		return !Physics.CheckCapsule(checkPos, humanPos, this._targettingThickness, FpcStateProcessor.Mask);
	}

	private Scp956Target.EffectReason CheckSpawnCondition(ReferenceHub hub)
	{
		if (!(hub.roleManager.CurrentRole is HumanRole))
		{
			return Scp956Target.EffectReason.None;
		}
		if (hub.playerEffectsController.GetEffect<Scp559Effect>().IsEnabled)
		{
			return Scp956Target.EffectReason.Child;
		}
		if (!hub.inventory.UserInventory.Items.Any((KeyValuePair<ushort, ItemBase> x) => x.Value.ItemTypeId == ItemType.SCP330))
		{
			return Scp956Target.EffectReason.None;
		}
		return Scp956Target.EffectReason.HasCandy;
	}

	private void UpdatePos()
	{
		Vector3 position = this._tr.position;
		Vector3 syncPos = this._syncPos;
		if (this._flying)
		{
			syncPos.y += Mathf.Sin(Time.timeSinceLevelLoad * this._flyingSinSpeed) * this._flyingSinIntensity;
		}
		if ((position - syncPos).sqrMagnitude > 9f)
		{
			this._tr.position = this._syncPos;
		}
		this._tr.SetPositionAndRotation(Vector3.Lerp(position, syncPos, Time.deltaTime * this._positionLerp), Quaternion.Lerp(this._tr.rotation, Quaternion.Euler(Vector3.up * this._syncRot), Time.deltaTime * this._rotationLerp));
		Scp956Pinata.LastPosition = position;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
		{
			Scp956Pinata.ActiveTargets.Remove(hub);
		}));
	}

	static Scp956Pinata()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Scp956Pinata), "System.Void Scp956Pinata::RpcAttack()", new RemoteCallDelegate(Scp956Pinata.InvokeUserCode_RpcAttack));
	}

	public override bool Weaved()
	{
		return true;
	}

	public bool Network_spawned
	{
		get
		{
			return this._spawned;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this._spawned, 1UL, null);
		}
	}

	public Vector3 Network_syncPos
	{
		get
		{
			return this._syncPos;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<Vector3>(value, ref this._syncPos, 2UL, null);
		}
	}

	public float Network_syncRot
	{
		get
		{
			return this._syncRot;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<float>(value, ref this._syncRot, 4UL, null);
		}
	}

	public bool Network_flying
	{
		get
		{
			return this._flying;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this._flying, 8UL, null);
		}
	}

	public byte Network_carpincho
	{
		get
		{
			return this._carpincho;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<byte>(value, ref this._carpincho, 16UL, null);
		}
	}

	protected void UserCode_RpcAttack()
	{
		this._attackTimer = 0f;
		this._attackSound.Play();
	}

	protected static void InvokeUserCode_RpcAttack(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAttack called on server.");
			return;
		}
		((Scp956Pinata)obj).UserCode_RpcAttack();
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this._spawned);
			writer.WriteVector3(this._syncPos);
			writer.WriteFloat(this._syncRot);
			writer.WriteBool(this._flying);
			writer.WriteByte(this._carpincho);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this._spawned);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteVector3(this._syncPos);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteFloat(this._syncRot);
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteBool(this._flying);
		}
		if ((base.syncVarDirtyBits & 16UL) != 0UL)
		{
			writer.WriteByte(this._carpincho);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this._spawned, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._syncPos, null, reader.ReadVector3());
			base.GeneratedSyncVarDeserialize<float>(ref this._syncRot, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize<bool>(ref this._flying, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize<byte>(ref this._carpincho, null, reader.ReadByte());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this._spawned, null, reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._syncPos, null, reader.ReadVector3());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<float>(ref this._syncRot, null, reader.ReadFloat());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this._flying, null, reader.ReadBool());
		}
		if ((num & 16L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this._carpincho, null, reader.ReadByte());
		}
	}

	[SyncVar]
	private bool _spawned;

	[SyncVar]
	private Vector3 _syncPos;

	[SyncVar]
	private float _syncRot;

	[SyncVar]
	private bool _flying;

	[SyncVar]
	private byte _carpincho;

	[SerializeField]
	private AnimationCurve _attackCurve;

	[SerializeField]
	private AudioSource _attackSound;

	[SerializeField]
	private float _positionLerp;

	[SerializeField]
	private float _rotationLerp;

	[SerializeField]
	private Material _appearMat;

	[SerializeField]
	private float _appearSpeed;

	[SerializeField]
	private float _heightTolerance;

	[SerializeField]
	private GameObject _visuals;

	[SerializeField]
	private GameObject _capybara;

	[SerializeField]
	private Transform _attackMover;

	[SerializeField]
	private float _changePositionCooldown;

	[SerializeField]
	private float _huntingTime;

	[SerializeField]
	private float _pinataRadius;

	[SerializeField]
	private float _doorOffset;

	[SerializeField]
	private float _raycastRange;

	[SerializeField]
	private float _raycastHeight;

	[SerializeField]
	private float _minDistance;

	[SerializeField]
	private float _floorHeight;

	[SerializeField]
	private float _targettingThickness;

	[SerializeField]
	private float _targettingRange;

	[SerializeField]
	private float _flyingSinIntensity;

	[SerializeField]
	private float _flyingSinSpeed;

	[SerializeField]
	private FacilityZone[] _spawnableZones;

	private float _fade;

	private float _respawnTimer;

	private float _sequenceTimer;

	private float _attackTimer;

	private float _initialRot;

	private Vector3 _spawnPos;

	private Vector3 _initialPos;

	private Transform _tr;

	private Scp956Target _foundTarget;

	private bool _attackTriggered;

	private const float SqrCutoffPoint = 9f;

	public static readonly HashSet<ReferenceHub> ActiveTargets = new HashSet<ReferenceHub>();

	private static readonly Dictionary<FacilityZone, int> ZonesByIntensity = new Dictionary<FacilityZone, int>();

	private static readonly List<DoorVariant> CompatibleDoors = new List<DoorVariant>();

	private static bool _hasBeenGiftSpawned = false;

	private static Scp956Pinata _instance;

	public static Vector3 LastPosition;
}
