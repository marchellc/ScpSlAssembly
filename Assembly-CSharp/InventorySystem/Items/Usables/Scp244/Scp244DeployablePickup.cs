using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioPooling;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public class Scp244DeployablePickup : CollisionDetectionPickup, IDestructible
	{
		private float GrowSpeed
		{
			get
			{
				return Time.deltaTime * (this.MaxDiameter / this.TimeToGrow);
			}
		}

		private float TimeToGrow
		{
			get
			{
				return 1f / this._growSpeedOverLifetime.Evaluate((float)this._lifeTime.Elapsed.TotalSeconds);
			}
		}

		private float CurTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		private Rigidbody Rb
		{
			get
			{
				return (base.PhysicsModule as PickupStandardPhysics).Rb;
			}
		}

		public bool ModelDestroyed
		{
			get
			{
				return this.State == Scp244State.Destroyed || this.State == Scp244State.PickedUp;
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

		public uint NetworkId
		{
			get
			{
				return base.netId;
			}
		}

		public Vector3 CenterOfMass
		{
			get
			{
				return this.Rb.worldCenterOfMass;
			}
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
					return;
				}
				main.simulationSpace = ParticleSystemSimulationSpace.Custom;
				main.customSimulationSpace = parent;
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
			if ((position - this._previousPos).sqrMagnitude < 1f)
			{
				return;
			}
			if (this._lastUpdateTime + 2.2f > this.CurTime)
			{
				return;
			}
			if (!SeedSynchronizer.MapGenerated)
			{
				return;
			}
			this.Conditions = Scp244TransferCondition.GenerateTransferConditions(this);
			this._previousPos = position;
			this._lastUpdateTime = this.CurTime;
			this._conditionsSet = true;
		}

		private void UpdateConditions()
		{
			if (!this._conditionsSet)
			{
				return;
			}
			bool flag = true;
			Bounds bounds = default(Bounds);
			foreach (Scp244TransferCondition scp244TransferCondition in this.Conditions)
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
						bounds = scp244TransferCondition.BoundsToEncapsulate;
					}
					else
					{
						bounds.Encapsulate(scp244TransferCondition.BoundsToEncapsulate);
					}
					flag = false;
				}
			}
			Bounds bounds2 = new Bounds(base.transform.position, Vector3.one * this.CurrentDiameter);
			bounds.SetMinMax(Vector3.Max(bounds2.min, bounds.min), Vector3.Min(bounds2.max, bounds.max));
			if ((this.CurrentBounds.center - bounds.center).sqrMagnitude < 100f)
			{
				Vector3 vector = this.CurrentBounds.size - bounds.size;
				float num = this.GrowSpeed;
				if (vector.x != 0f && vector.z != 0f)
				{
					num *= 2f;
				}
				else
				{
					num /= 2f;
				}
				Vector3 vector2 = Vector3.MoveTowards(this.CurrentBounds.center, bounds.center, num / 2f);
				Vector3 vector3 = Vector3.MoveTowards(this.CurrentBounds.size, bounds.size, num);
				this.CurrentBounds = new Bounds(vector2, vector3);
				return;
			}
			this.CurrentBounds = bounds;
		}

		private void UpdateRange()
		{
			if (this.ModelDestroyed && this._visibleModel.activeSelf)
			{
				this.Rb.constraints = RigidbodyConstraints.FreezeAll;
				this._visibleModel.SetActive(false);
				this._emissionSoundSource.enabled = false;
				if (this.State == Scp244State.Destroyed)
				{
					AudioSourcePoolManager.PlayOnTransform(this._destroyClips.RandomItem<AudioClip>(), base.transform, this.MaxDiameter, 1f, FalloffType.Exponential, MixerChannel.Weapons, 1f);
					Transform transform = global::UnityEngine.Object.Instantiate<GameObject>(this._destroyedModel).transform;
					transform.transform.SetPositionAndRotation(base.transform.position, base.transform.rotation);
					transform.localScale = Vector3.one;
					foreach (Rigidbody rigidbody in transform.GetComponentsInChildren<Rigidbody>())
					{
						global::UnityEngine.Object.Destroy(rigidbody.GetComponent<Collider>(), this._timeToDecay);
						global::UnityEngine.Object.Destroy(rigidbody, this._timeToDecay);
					}
				}
			}
			if (!NetworkServer.active)
			{
				this.CurrentSizePercent = (float)this._syncSizePercent;
				this.CurrentSizePercent /= 255f;
				return;
			}
			if (this.State == Scp244State.Idle && Vector3.Dot(base.transform.up, Vector3.up) < this._activationDot)
			{
				this.State = Scp244State.Active;
				this._lifeTime.Restart();
			}
			float num = ((this.State == Scp244State.Active) ? this.TimeToGrow : (-this._timeToDecay));
			this.CurrentSizePercent = Mathf.Clamp01(this.CurrentSizePercent + Time.deltaTime / num);
			this.Network_syncSizePercent = (byte)Mathf.RoundToInt(this.CurrentSizePercent * 255f);
			if (!this.ModelDestroyed || this.CurrentSizePercent > 0f)
			{
				return;
			}
			this._timeToDecay -= Time.deltaTime;
			if (this._timeToDecay <= 0f)
			{
				NetworkServer.Destroy(base.gameObject);
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
			this._mainEffect.shape.mesh = this._generatedMesh;
		}

		private void UpdateEffects()
		{
			bool enabled = this._emissionSoundSource.enabled;
			bool flag = this.State == Scp244State.Active;
			if (enabled != flag)
			{
				this._emissionSoundSource.enabled = flag;
				this._lifeTime.Restart();
			}
			this._mainEffect.emission.rateOverTimeMultiplier = this._emissionOverPercent.Evaluate(this.CurrentSizePercent);
			float num = this.CurrentDiameter / 2f - 3.5f;
			float num2 = this._sizeOverDiameter.Evaluate(this.CurrentDiameter);
			this._mainEffect.main.startSize = new ParticleSystem.MinMaxCurve(this._initialSize.x * num2, this._initialSize.y * num2);
			if (this.State == Scp244State.Idle)
			{
				return;
			}
			Bounds currentBounds = this.CurrentBounds;
			currentBounds.size -= new Vector3(3.5f, 0f, 3.5f);
			currentBounds.center -= base.transform.position;
			for (int i = 0; i < 30; i++)
			{
				Vector3 vector = this._templateVerticles[this._particleTimer] * num;
				this._updatedVerticles[this._particleTimer] = currentBounds.ClosestPoint(vector);
				int num3 = this._particleTimer + 1;
				this._particleTimer = num3;
				if (num3 >= this._meshVertsCount)
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
			bounds.Expand(-this._fullSubmergeDistance);
			float num5 = Vector3.Distance(bounds.ClosestPoint(worldPoint), worldPoint);
			float num6 = num3 - num4 + this._fullSubmergeDistance;
			float num7 = 1f - Mathf.Clamp01(Mathf.Max(num5, num6) / this._transitionDistance);
			if (this.ModelDestroyed)
			{
				num7 *= this.CurrentSizePercent;
			}
			if (num < this._minimalInfluenceDistance)
			{
				num7 *= num / this._minimalInfluenceDistance;
			}
			return num7;
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

		public byte Network_syncSizePercent
		{
			get
			{
				return this._syncSizePercent;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncSizePercent, 2UL, null);
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
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncState, 4UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this._syncSizePercent);
				writer.WriteByte(this._syncState);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteByte(this._syncSizePercent);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteByte(this._syncState);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncSizePercent, null, reader.ReadByte());
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncState, null, reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncSizePercent, null, reader.ReadByte());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncState, null, reader.ReadByte());
			}
		}

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
	}
}
