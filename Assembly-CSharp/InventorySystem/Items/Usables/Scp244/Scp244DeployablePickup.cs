using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioPooling;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using InventorySystem.Searching;
using MapGeneration;
using MapGeneration.StaticHelpers;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244;

public class Scp244DeployablePickup : CollisionDetectionPickup, IDestructible, IBlockStaticBatching
{
	private const float SquaredDisUpdateDiff = 1f;

	private const float ForceBoundsUpdateSqrtDiff = 100f;

	private const float UpdateCooldownTime = 2.2f;

	private const int VertsPerFrame = 30;

	private const float ParticleSize = 3.5f;

	public static readonly HashSet<Scp244DeployablePickup> Instances = new HashSet<Scp244DeployablePickup>();

	public float MaxDiameter;

	public AnimationCurve FogDistanceCurve;

	public AnimationCurve FogLerpCurve;

	[SyncVar]
	private byte _syncSizePercent;

	[SyncVar]
	private byte _syncState;

	[SerializeField]
	private AnimationCurve _growSpeedOverLifetime;

	[SerializeField]
	private float _timeToDecay;

	[SerializeField]
	private float _transitionDistance;

	[SerializeField]
	private float _fullSubmergeDistance;

	[SerializeField]
	private GameObject _visibleModel;

	[SerializeField]
	private float _minimalInfluenceDistance;

	[SerializeField]
	private float _activationDot;

	[SerializeField]
	private float _health;

	[SerializeField]
	private float _deployedPickupTime;

	[SerializeField]
	private float _heightRadiusRatio;

	[SerializeField]
	private ParticleSystem _mainEffect;

	[SerializeField]
	private GameObject _destroyedModel;

	[SerializeField]
	private Mesh _referenceMesh;

	[SerializeField]
	private AnimationCurve _emissionOverPercent;

	[SerializeField]
	private AnimationCurve _sizeOverDiameter;

	[SerializeField]
	private AudioClip[] _destroyClips;

	[SerializeField]
	private AudioSource _emissionSoundSource;

	private Vector3[] _templateVerticles;

	private Vector3[] _updatedVerticles;

	private int _meshVertsCount;

	private int _particleTimer;

	private Mesh _generatedMesh;

	private Vector2 _initialSize;

	private Vector3 _previousPos;

	private float _lastActiveSize;

	private float _lastUpdateTime;

	private bool _conditionsSet;

	private readonly Stopwatch _lifeTime = Stopwatch.StartNew();

	private float GrowSpeed => Time.deltaTime * (this.MaxDiameter / this.TimeToGrow);

	private float TimeToGrow => 1f / this._growSpeedOverLifetime.Evaluate((float)this._lifeTime.Elapsed.TotalSeconds);

	private float CurTime => Time.timeSinceLevelLoad;

	private Rigidbody Rb => (base.PhysicsModule as PickupStandardPhysics).Rb;

	public bool ModelDestroyed
	{
		get
		{
			if (this.State != Scp244State.Destroyed)
			{
				return this.State == Scp244State.PickedUp;
			}
			return true;
		}
	}

	public float CurrentDiameter
	{
		get
		{
			if (this.State == Scp244State.Active)
			{
				this._lastActiveSize = this.CurrentSizePercent * this.MaxDiameter;
			}
			return this._lastActiveSize;
		}
	}

	public Bounds CurrentBounds { get; private set; }

	public float CurrentSizePercent { get; private set; }

	public Scp244TransferCondition[] Conditions { get; private set; }

	public Scp244State State
	{
		get
		{
			return (Scp244State)this._syncState;
		}
		set
		{
			this.Network_syncState = (byte)value;
		}
	}

	public uint NetworkId => base.netId;

	public Vector3 CenterOfMass => this.Rb.worldCenterOfMass;

	public byte Network_syncSizePercent
	{
		get
		{
			return this._syncSizePercent;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncSizePercent, 2uL, null);
		}
	}

	public byte Network_syncState
	{
		get
		{
			return this._syncState;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncState, 4uL, null);
		}
	}

	public override PickupSearchCompletor GetPickupSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		if (!InventoryItemLoader.TryGetItem<ItemBase>(base.Info.ItemId, out var result))
		{
			return null;
		}
		return new Scp244SearchCompletor(coordinator.Hub, this, result, sqrDistance);
	}

	private void Update()
	{
		this.UpdateCurrentRoom();
		this.UpdateConditions();
		this.UpdateRange();
		this.UpdateEffects();
	}

	protected override void Awake()
	{
		base.Awake();
		(base.PhysicsModule as PickupStandardPhysics).OnParentSetByElevator += delegate
		{
			Transform parent = base.transform.parent;
			ParticleSystem.MainModule main = this._mainEffect.main;
			if (parent == null)
			{
				main.simulationSpace = ParticleSystemSimulationSpace.World;
			}
			else
			{
				main.simulationSpace = ParticleSystemSimulationSpace.Custom;
				main.customSimulationSpace = parent;
			}
		};
	}

	protected override void Start()
	{
		base.Start();
		Scp244DeployablePickup.Instances.Add(this);
		this.SetupEffects();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp244DeployablePickup.Instances.Remove(this);
	}

	private void UpdateCurrentRoom()
	{
		Vector3 position = base.transform.position;
		if (!((position - this._previousPos).sqrMagnitude < 1f) && !(this._lastUpdateTime + 2.2f > this.CurTime) && SeedSynchronizer.MapGenerated)
		{
			this.Conditions = Scp244TransferCondition.GenerateTransferConditions(this);
			this._previousPos = position;
			this._lastUpdateTime = this.CurTime;
			this._conditionsSet = true;
		}
	}

	private void UpdateConditions()
	{
		if (!this._conditionsSet)
		{
			return;
		}
		bool flag = true;
		Bounds currentBounds = default(Bounds);
		Scp244TransferCondition[] conditions = this.Conditions;
		foreach (Scp244TransferCondition scp244TransferCondition in conditions)
		{
			bool flag2 = true;
			DoorVariant[] doors = scp244TransferCondition.Doors;
			for (int j = 0; j < doors.Length; j++)
			{
				if (!doors[j].IsConsideredOpen())
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				if (flag)
				{
					currentBounds = scp244TransferCondition.BoundsToEncapsulate;
				}
				else
				{
					currentBounds.Encapsulate(scp244TransferCondition.BoundsToEncapsulate);
				}
				flag = false;
			}
		}
		Bounds bounds = new Bounds(base.transform.position, Vector3.one * this.CurrentDiameter);
		currentBounds.SetMinMax(Vector3.Max(bounds.min, currentBounds.min), Vector3.Min(bounds.max, currentBounds.max));
		if ((this.CurrentBounds.center - currentBounds.center).sqrMagnitude < 100f)
		{
			Vector3 vector = this.CurrentBounds.size - currentBounds.size;
			float growSpeed = this.GrowSpeed;
			growSpeed = ((vector.x == 0f || vector.z == 0f) ? (growSpeed / 2f) : (growSpeed * 2f));
			Vector3 center = Vector3.MoveTowards(this.CurrentBounds.center, currentBounds.center, growSpeed / 2f);
			Vector3 size = Vector3.MoveTowards(this.CurrentBounds.size, currentBounds.size, growSpeed);
			this.CurrentBounds = new Bounds(center, size);
		}
		else
		{
			this.CurrentBounds = currentBounds;
		}
	}

	private void UpdateRange()
	{
		if (this.ModelDestroyed && this._visibleModel.activeSelf)
		{
			this.Rb.constraints = RigidbodyConstraints.FreezeAll;
			this._visibleModel.SetActive(value: false);
			this._emissionSoundSource.enabled = false;
			if (this.State == Scp244State.Destroyed)
			{
				AudioSourcePoolManager.PlayOnTransform(this._destroyClips.RandomItem(), base.transform, this.MaxDiameter, 1f, FalloffType.Exponential, MixerChannel.Weapons);
				Transform obj = Object.Instantiate(this._destroyedModel).transform;
				obj.transform.SetPositionAndRotation(base.transform.position, base.transform.rotation);
				obj.localScale = Vector3.one;
				Rigidbody[] componentsInChildren = obj.GetComponentsInChildren<Rigidbody>();
				foreach (Rigidbody obj2 in componentsInChildren)
				{
					Object.Destroy(obj2.GetComponent<Collider>(), this._timeToDecay);
					Object.Destroy(obj2, this._timeToDecay);
				}
			}
		}
		if (!NetworkServer.active)
		{
			this.CurrentSizePercent = (int)this._syncSizePercent;
			this.CurrentSizePercent /= 255f;
			return;
		}
		if (this.State == Scp244State.Idle && Vector3.Dot(base.transform.up, Vector3.up) < this._activationDot)
		{
			this.State = Scp244State.Active;
			this._lifeTime.Restart();
		}
		float num = ((this.State == Scp244State.Active) ? this.TimeToGrow : (0f - this._timeToDecay));
		this.CurrentSizePercent = Mathf.Clamp01(this.CurrentSizePercent + Time.deltaTime / num);
		this.Network_syncSizePercent = (byte)Mathf.RoundToInt(this.CurrentSizePercent * 255f);
		if (this.ModelDestroyed && !(this.CurrentSizePercent > 0f))
		{
			this._timeToDecay -= Time.deltaTime;
			if (this._timeToDecay <= 0f)
			{
				NetworkServer.Destroy(base.gameObject);
			}
		}
	}

	private void SetupEffects()
	{
		this._templateVerticles = this._referenceMesh.vertices;
		this._updatedVerticles = this._referenceMesh.vertices;
		this._meshVertsCount = this._templateVerticles.Length;
		ParticleSystem.MainModule main = this._mainEffect.main;
		this._initialSize = new Vector2(main.startSize.constantMin, main.startSize.constantMax);
		this._generatedMesh = new Mesh
		{
			vertices = new Vector3[this._meshVertsCount],
			triangles = this._referenceMesh.triangles,
			normals = this._referenceMesh.normals
		};
		ParticleSystem.ShapeModule shape = this._mainEffect.shape;
		shape.mesh = this._generatedMesh;
	}

	private void UpdateEffects()
	{
		bool num = this._emissionSoundSource.enabled;
		bool flag = this.State == Scp244State.Active;
		if (num != flag)
		{
			this._emissionSoundSource.enabled = flag;
			this._lifeTime.Restart();
		}
		ParticleSystem.EmissionModule emission = this._mainEffect.emission;
		emission.rateOverTimeMultiplier = this._emissionOverPercent.Evaluate(this.CurrentSizePercent);
		float num2 = this.CurrentDiameter / 2f - 3.5f;
		float num3 = this._sizeOverDiameter.Evaluate(this.CurrentDiameter);
		ParticleSystem.MainModule main = this._mainEffect.main;
		main.startSize = new ParticleSystem.MinMaxCurve(this._initialSize.x * num3, this._initialSize.y * num3);
		if (this.State == Scp244State.Idle)
		{
			return;
		}
		Bounds currentBounds = this.CurrentBounds;
		currentBounds.size -= new Vector3(3.5f, 0f, 3.5f);
		currentBounds.center -= base.transform.position;
		for (int i = 0; i < 30; i++)
		{
			Vector3 point = this._templateVerticles[this._particleTimer] * num2;
			this._updatedVerticles[this._particleTimer] = currentBounds.ClosestPoint(point);
			if (++this._particleTimer >= this._meshVertsCount)
			{
				this._particleTimer = 0;
			}
		}
		this._generatedMesh.vertices = this._updatedVerticles;
		this._mainEffect.transform.rotation = Quaternion.identity;
	}

	public float FogPercentForPoint(Vector3 worldPoint)
	{
		if (this.State == Scp244State.Idle)
		{
			return 0f;
		}
		Vector3 vector = base.transform.position - worldPoint;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = this.CurrentDiameter * 0.5f;
		float num2 = num + this._transitionDistance;
		if (sqrMagnitude > num2 * num2)
		{
			return 0f;
		}
		float num3 = Mathf.Sqrt(sqrMagnitude);
		Vector3 vector2 = ((num3 == 0f) ? Vector3.zero : (vector / num3));
		float num4 = Mathf.LerpUnclamped(num, num * this._heightRadiusRatio, Mathf.Abs(vector2.y));
		Bounds bounds = new Bounds(this.CurrentBounds.center, this.CurrentBounds.size);
		bounds.Expand(0f - this._fullSubmergeDistance);
		float a = Vector3.Distance(bounds.ClosestPoint(worldPoint), worldPoint);
		float b = num3 - num4 + this._fullSubmergeDistance;
		float num5 = 1f - Mathf.Clamp01(Mathf.Max(a, b) / this._transitionDistance);
		if (this.ModelDestroyed)
		{
			num5 *= this.CurrentSizePercent;
		}
		if (num < this._minimalInfluenceDistance)
		{
			num5 *= num / this._minimalInfluenceDistance;
		}
		return num5;
	}

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
	{
		if (!(handler is ExplosionDamageHandler))
		{
			return false;
		}
		if (this._health <= 0f || this.ModelDestroyed)
		{
			return false;
		}
		this._health -= damage;
		if (this._health <= 0f)
		{
			this.State = Scp244State.Destroyed;
		}
		return true;
	}

	public override float SearchTimeForPlayer(ReferenceHub hub)
	{
		float num = base.SearchTimeForPlayer(hub);
		if (this.State == Scp244State.Active)
		{
			num += this._deployedPickupTime;
		}
		return num;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncSizePercent);
			NetworkWriterExtensions.WriteByte(writer, this._syncState);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncSizePercent);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncState);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncSizePercent, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncState, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncSizePercent, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncState, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
