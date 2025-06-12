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

	public static readonly HashSet<ReferenceHub> ActiveTargets;

	private static readonly Dictionary<FacilityZone, int> ZonesByIntensity;

	private static readonly List<DoorVariant> CompatibleDoors;

	private static bool _hasBeenGiftSpawned;

	private static Scp956Pinata _instance;

	public static Vector3 LastPosition;

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

	private static float RandomSign => (Random.value > 0.5f) ? 1 : (-1);

	public bool Network_spawned
	{
		get
		{
			return this._spawned;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._spawned, 1uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._syncPos, 2uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._syncRot, 4uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._flying, 8uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._carpincho, 16uL, null);
		}
	}

	public static bool TryGetInstance(out Scp956Pinata instance)
	{
		instance = Scp956Pinata._instance;
		return instance != null;
	}

	[ClientRpc]
	private void RpcAttack()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Scp956Pinata::RpcAttack()", 977535511, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
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
				this._capybara.SetActive(value: true);
			}
			else
			{
				this._visuals.SetActive(value: true);
			}
		}
		this._tr.rotation = Quaternion.Euler(Vector3.up * this._syncRot);
	}

	private void UpdateAi()
	{
		this._respawnTimer -= Time.deltaTime;
		if (!this._spawned && !Scp956Pinata._hasBeenGiftSpawned)
		{
			if (!(this._respawnTimer > 0f))
			{
				if (!this.TryGetSpawnPos(out var pos))
				{
					this._respawnTimer = this._changePositionCooldown;
					return;
				}
				this.Network_carpincho = (byte)Random.Range(0, 255);
				this.Network_syncPos = pos;
				this._spawnPos = pos;
				this.Network_syncRot = Random.value * 360f;
				this.Network_spawned = true;
				this.Network_flying = false;
				this._respawnTimer = this._huntingTime;
			}
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			Scp956Target.EffectReason effectReason = this.CheckSpawnCondition(allHub);
			if (effectReason == Scp956Target.EffectReason.None || !this.CheckPlayer(allHub))
			{
				if (Scp956Pinata.ActiveTargets.Remove(allHub))
				{
					allHub.playerEffectsController.DisableEffect<Scp956Target>();
				}
			}
			else
			{
				Scp956Pinata.ActiveTargets.Add(allHub);
				allHub.playerEffectsController.GetEffect<Scp956Target>().Intensity = (byte)effectReason;
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
			foreach (ReferenceHub activeTarget in Scp956Pinata.ActiveTargets)
			{
				Scp956Target effect = activeTarget.playerEffectsController.GetEffect<Scp956Target>();
				if (effect.IsAffected)
				{
					float sqrMagnitude = (effect.Position - this._tr.position).sqrMagnitude;
					if (!(sqrMagnitude > num))
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
		float b = Vector3.Angle(normalized, Vector3.forward) * Mathf.Sign(Vector3.Dot(normalized, Vector3.right));
		this.Network_syncRot = Mathf.LerpAngle(this._initialRot, b, (this._sequenceTimer - 1f) / 4f);
		if (this._sequenceTimer < 6f)
		{
			return;
		}
		Vector3 b2 = this._foundTarget.Position - this._tr.forward.NormalizeIgnoreY();
		if (Mathf.Abs(this._spawnPos.y - this._foundTarget.Position.y) < this._heightTolerance)
		{
			b2.y = this._spawnPos.y;
			this.Network_flying = false;
		}
		else
		{
			b2.y = this._foundTarget.Position.y;
			this.Network_flying = true;
		}
		this.Network_syncPos = Vector3.Lerp(this._initialPos, b2, (this._sequenceTimer - 6f) / 7f);
		if (!(this._sequenceTimer < 13.3f))
		{
			if (!this._attackTriggered)
			{
				this.RpcAttack();
				this._attackTriggered = true;
			}
			if (!(this._sequenceTimer < 13.5f))
			{
				this._foundTarget.Hub.playerStats.DealDamage(new Scp956DamageHandler(normalized));
				this._foundTarget = null;
			}
		}
	}

	public void SpawnBehindTarget(ReferenceHub target)
	{
		Scp956Pinata.ActiveTargets.Add(target);
		this._foundTarget = target.playerEffectsController.EnableEffect<Scp956Target>();
		this._foundTarget.Intensity = 254;
		this._respawnTimer = this._huntingTime;
		Transform transform = target.transform;
		Vector3 initialPos = (this.Network_syncPos = (this._spawnPos = transform.position - transform.forward * 2f));
		this._initialPos = initialPos;
		float initialRot = (this.Network_syncRot = Random.value * 360f);
		this._initialRot = initialRot;
		this._respawnTimer = this._huntingTime;
		this._attackTriggered = false;
		bool hasBeenGiftSpawned = (this.Network_spawned = true);
		Scp956Pinata._hasBeenGiftSpawned = hasBeenGiftSpawned;
	}

	private void TryDespawn()
	{
		Vector3 thisPos = this._tr.position;
		if (!ReferenceHub.AllHubs.Any((ReferenceHub x) => x.roleManager.CurrentRole is IFpcRole fpcRole && this.CheckVisibility(thisPos, fpcRole.FpcModule.Position)))
		{
			this.Network_spawned = false;
			this._respawnTimer = this._changePositionCooldown;
		}
	}

	private bool CheckPlayer(ReferenceHub hub)
	{
		Vector3 position = this._tr.position;
		Vector3 position2 = (hub.roleManager.CurrentRole as HumanRole).FpcModule.Position;
		float num = this._targettingRange * this._targettingRange;
		if ((position - position2).sqrMagnitude > num)
		{
			return false;
		}
		if (!this.CheckVisibility(position, position2))
		{
			return false;
		}
		if (!WaypointBase.TryGetWaypoint(new RelativePosition(position2).WaypointId, out var wp))
		{
			return true;
		}
		return !(wp is ElevatorWaypoint);
	}

	private bool TryGetSpawnPos(out Vector3 pos)
	{
		pos = default(Vector3);
		Scp956Pinata.ZonesByIntensity.Clear();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (this.CheckSpawnCondition(allHub) == Scp956Target.EffectReason.Child && allHub.roleManager.CurrentRole is HumanRole && allHub.TryGetCurrentRoom(out var room) && this._spawnableZones.Contains(room.Zone))
			{
				Scp956Pinata.ZonesByIntensity.TryGetValue(room.Zone, out var value);
				Scp956Pinata.ZonesByIntensity[room.Zone] = value + 1;
			}
		}
		if (Scp956Pinata.ZonesByIntensity.Count == 0)
		{
			return false;
		}
		FacilityZone facilityZone = FacilityZone.None;
		int num = 0;
		foreach (KeyValuePair<FacilityZone, int> item in Scp956Pinata.ZonesByIntensity)
		{
			if (item.Value >= num)
			{
				facilityZone = item.Key;
				num = item.Value;
			}
		}
		Scp956Pinata.CompatibleDoors.Clear();
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			if (allDoor is BreakableDoor && allDoor.Rooms != null && allDoor.Rooms.Length != 0 && allDoor.Rooms[0].Zone == facilityZone)
			{
				Scp956Pinata.CompatibleDoors.Add(allDoor);
			}
		}
		while (Scp956Pinata.CompatibleDoors.Count > 0)
		{
			int index = Random.Range(0, Scp956Pinata.CompatibleDoors.Count);
			Transform transform = Scp956Pinata.CompatibleDoors[index].transform;
			Scp956Pinata.CompatibleDoors.RemoveAt(index);
			pos = transform.position + this._doorOffset * Scp956Pinata.RandomSign * transform.forward + Vector3.up * this._raycastHeight;
			if (Physics.SphereCast(pos, this._pinataRadius, transform.right * Scp956Pinata.RandomSign, out var hitInfo, this._raycastRange, FpcStateProcessor.Mask) && !(hitInfo.distance < this._minDistance))
			{
				Vector3 vector = hitInfo.point + hitInfo.normal * this._pinataRadius;
				pos = new Vector3(vector.x, transform.position.y + this._floorHeight, vector.z);
				Vector3 checkPos = pos;
				if (!ReferenceHub.AllHubs.Any((ReferenceHub x) => x.roleManager.CurrentRole is IFpcRole fpcRole && this.CheckVisibility(checkPos, fpcRole.FpcModule.Position)))
				{
					return true;
				}
			}
		}
		return false;
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
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			Scp956Pinata.ActiveTargets.Remove(hub);
		};
	}

	static Scp956Pinata()
	{
		Scp956Pinata.ActiveTargets = new HashSet<ReferenceHub>();
		Scp956Pinata.ZonesByIntensity = new Dictionary<FacilityZone, int>();
		Scp956Pinata.CompatibleDoors = new List<DoorVariant>();
		Scp956Pinata._hasBeenGiftSpawned = false;
		RemoteProcedureCalls.RegisterRpc(typeof(Scp956Pinata), "System.Void Scp956Pinata::RpcAttack()", InvokeUserCode_RpcAttack);
	}

	public override bool Weaved()
	{
		return true;
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
		}
		else
		{
			((Scp956Pinata)obj).UserCode_RpcAttack();
		}
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
			NetworkWriterExtensions.WriteByte(writer, this._carpincho);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this._spawned);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteVector3(this._syncPos);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteFloat(this._syncRot);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteBool(this._flying);
		}
		if ((base.syncVarDirtyBits & 0x10L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._carpincho);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._spawned, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._syncPos, null, reader.ReadVector3());
			base.GeneratedSyncVarDeserialize(ref this._syncRot, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._flying, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._carpincho, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._spawned, null, reader.ReadBool());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncPos, null, reader.ReadVector3());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncRot, null, reader.ReadFloat());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._flying, null, reader.ReadBool());
		}
		if ((num & 0x10L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._carpincho, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
