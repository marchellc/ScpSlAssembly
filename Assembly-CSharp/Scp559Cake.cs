using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AudioPooling;
using GameCore;
using Interactables;
using Interactables.Verification;
using MapGeneration;
using Mirror;
using PlayerRoles;
using UnityEngine;

public class Scp559Cake : NetworkBehaviour, IServerInteractable, IInteractable
{
	public IVerificationRule VerificationRule
	{
		get
		{
			return StandardDistanceVerification.Default;
		}
	}

	public bool CanBeUsed
	{
		get
		{
			return this._remainingSlices > 0;
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!this._isSpawned || !ply.IsHuman() || this._remainingSlices == 0)
		{
			return;
		}
		Scp559Effect effect = ply.playerEffectsController.GetEffect<Scp559Effect>();
		if (effect.IsEnabled)
		{
			return;
		}
		this.Network_remainingSlices = this._remainingSlices - 1;
		effect.IsEnabled = true;
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.UpdateServer();
		}
		this.UpdateVisual();
	}

	private void Start()
	{
		if (NetworkServer.active && !this.CanSpawn())
		{
			NetworkServer.Destroy(base.gameObject);
			return;
		}
		this.SetPosition(this._position, this._position);
	}

	private void SetPosition(Vector3 prev, Vector3 cur)
	{
		base.transform.position = cur;
	}

	private void UpdateServer()
	{
		this._remainingTime -= Time.deltaTime;
		if (this._remainingTime > 0f)
		{
			return;
		}
		if (this._isSpawned)
		{
			this.Network_isSpawned = false;
			this._remainingTime = this._respawnTime;
			return;
		}
		if (!RoundStart.RoundStarted)
		{
			return;
		}
		if (RoundStart.RoundLength.TotalSeconds < (double)this._respawnTime)
		{
			return;
		}
		Vector3 vector;
		bool flag;
		if (!this.TryGetSpawnPoint(out vector, out flag))
		{
			return;
		}
		this.Network_position = vector;
		this.Network_isSpawned = true;
		this.Network_hasPedestal = flag;
		this.Network_remainingSlices = this._maxSlices;
		this._remainingTime = this._spawnedDuration;
	}

	private void UpdateVisual()
	{
		this._pedestal.SetActive(this._hasPedestal);
		float num = Mathf.Clamp01(this._fade + this._dissolveSpeed * Time.deltaTime * (float)(this._isSpawned ? 1 : (-1)));
		if (num != this._fade)
		{
			if (this._fade <= 0f)
			{
				AudioSourcePoolManager.PlayOnTransform(this._appearSound, base.transform, 25f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			}
			if (this._fade >= 1f)
			{
				AudioSourcePoolManager.PlayOnTransform(this._disappearClip, base.transform, 15f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			}
			this._fade = num;
			this._dissolveMat.SetFloat(Scp559Cake.ShaderDissolveProperty, 1f - this._fade);
			this._visuals.SetActive(this._fade > 0f);
			this._prevSlices = -1;
			this._slices.ForEach(delegate(GameObject x)
			{
				x.SetActive(false);
			});
			return;
		}
		if (this._prevSlices != (int)this._remainingSlices)
		{
			for (int i = 0; i < this._slices.Length; i++)
			{
				this._slices[i].SetActive(i < (int)this._remainingSlices);
			}
			if (this._prevSlices > (int)this._remainingSlices)
			{
				AudioSourcePoolManager.PlayOnTransform(this._useSound, base.transform, 9f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			}
			this._prevSlices = (int)this._remainingSlices;
		}
	}

	private bool TryGetSpawnPoint(out Vector3 pos, out bool pedestal)
	{
		Scp559Cake.PossiblePositions.Clear();
		Scp559Cake.PopulatedRooms.Clear();
		int num = 0;
		foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
		{
			HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
			if (humanRole != null)
			{
				RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(humanRole.FpcModule.Position, true);
				if (!(roomIdentifier == null) && Scp559Cake.Spawnpoints.ContainsKey(roomIdentifier.Name))
				{
					int num2;
					Scp559Cake.PopulatedRooms.TryGetValue(roomIdentifier, out num2);
					if (!referenceHub.playerEffectsController.GetEffect<Scp559Effect>().IsEnabled)
					{
						num2++;
					}
					Scp559Cake.PopulatedRooms[roomIdentifier] = num2;
					num = Mathf.Max(num2, num);
				}
			}
		}
		foreach (KeyValuePair<RoomIdentifier, int> keyValuePair in Scp559Cake.PopulatedRooms)
		{
			if (keyValuePair.Value >= num)
			{
				Vector4 vector = Scp559Cake.Spawnpoints[keyValuePair.Key.Name];
				Vector3 vector2 = keyValuePair.Key.transform.TransformPoint(vector);
				if (!Physics.CheckSphere(vector2, this._cakeRadius, Scp559Cake.CheckerMask))
				{
					Scp559Cake.PossiblePositions.Add(new Vector4(vector2.x, vector2.y, vector2.z, vector.w));
				}
			}
		}
		if (Scp559Cake.PossiblePositions.Count == 0)
		{
			pos = Vector3.zero;
			pedestal = false;
			return false;
		}
		Vector4 vector3 = Scp559Cake.PossiblePositions.RandomItem<Vector4>();
		pos = vector3;
		pedestal = vector3.w > 0f;
		if (!pedestal)
		{
			pos += Scp559Cake.PedestalOffset;
		}
		return true;
	}

	private bool CanSpawn()
	{
		if (SeedSynchronizer.Seed == 559)
		{
			return true;
		}
		global::System.Random random = new global::System.Random(SeedSynchronizer.Seed);
		return 33.0 >= random.NextDouble() * 100.0;
	}

	// Note: this type is marked as 'beforefieldinit'.
	static Scp559Cake()
	{
		Dictionary<RoomName, Vector4> dictionary = new Dictionary<RoomName, Vector4>();
		dictionary[RoomName.LczClassDSpawn] = new Vector4(-19f, 0f, 0f, 1f);
		dictionary[RoomName.LczToilets] = new Vector4(-5.3f, 0.95f, -6.5f, 0f);
		dictionary[RoomName.LczGreenhouse] = new Vector4(0f, 0f, -5.5f, 1f);
		dictionary[RoomName.LczComputerRoom] = new Vector4(6f, 0f, 4.5f, 1f);
		dictionary[RoomName.LczGlassroom] = new Vector4(8.3f, 1.05f, -5.9f, 0f);
		dictionary[RoomName.LczArmory] = new Vector4(4.14f, 0.82f, -0.95f, 0f);
		dictionary[RoomName.Lcz330] = new Vector4(2f, 0.91f, 0.7f, 0f);
		dictionary[RoomName.Lcz173] = new Vector4(-2.56f, 12.32f, -4.67f, 0f);
		dictionary[RoomName.Lcz914] = new Vector4(1.1f, 1.011f, -7.16f, 0f);
		dictionary[RoomName.LczAirlock] = new Vector4(0.65f, 0f, -4.6f, 1f);
		dictionary[RoomName.HczServers] = new Vector4(2f, 0f, -0.35f, 1f);
		dictionary[RoomName.HczWarhead] = new Vector4(0.65f, 291.89f, 10.51f, 0f);
		dictionary[RoomName.HczArmory] = new Vector4(0.89f, 0.88f, -1.44f, 0f);
		dictionary[RoomName.HczMicroHID] = new Vector4(2.3f, 0.92f, -5.27f, 0f);
		dictionary[RoomName.Hcz049] = new Vector4(-5.65f, 192.44f, -1.75f, 1f);
		dictionary[RoomName.Hcz079] = new Vector4(1.8f, -3.33f, -6.3f, 1f);
		dictionary[RoomName.Hcz096] = new Vector4(-4.4f, 0f, 1f, 1f);
		dictionary[RoomName.Hcz106] = new Vector4(24.38f, 0.86f, -15.54f, 0f);
		dictionary[RoomName.Hcz939] = new Vector4(0.53f, 1.05f, 2.9f, 0f);
		dictionary[RoomName.HczTestroom] = new Vector4(0.915f, 0.74f, -4.7f, 0f);
		dictionary[RoomName.HczCheckpointToEntranceZone] = new Vector4(-6.07f, 0f, -3.35f, 1f);
		dictionary[RoomName.EzOfficeStoried] = new Vector4(-2.26f, 0.89f, 0.4f, 0f);
		dictionary[RoomName.EzOfficeLarge] = new Vector4(1.14f, 0.89f, 0.29f, 0f);
		dictionary[RoomName.EzOfficeSmall] = new Vector4(6f, -0.54f, 2.7f, 0f);
		dictionary[RoomName.EzIntercom] = new Vector4(-6.9f, -5.04f, -2.8f, 0f);
		dictionary[RoomName.EzGateA] = new Vector4(2.9f, 0.91f, 1.19f, 0f);
		dictionary[RoomName.EzGateB] = new Vector4(3.38f, 0.91f, -2.8f, 0f);
		Scp559Cake.Spawnpoints = dictionary;
	}

	public override bool Weaved()
	{
		return true;
	}

	public byte Network_remainingSlices
	{
		get
		{
			return this._remainingSlices;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<byte>(value, ref this._remainingSlices, 1UL, null);
		}
	}

	public bool Network_isSpawned
	{
		get
		{
			return this._isSpawned;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this._isSpawned, 2UL, null);
		}
	}

	public bool Network_hasPedestal
	{
		get
		{
			return this._hasPedestal;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this._hasPedestal, 4UL, null);
		}
	}

	public Vector3 Network_position
	{
		get
		{
			return this._position;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<Vector3>(value, ref this._position, 8UL, new Action<Vector3, Vector3>(this.SetPosition));
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteByte(this._remainingSlices);
			writer.WriteBool(this._isSpawned);
			writer.WriteBool(this._hasPedestal);
			writer.WriteVector3(this._position);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteByte(this._remainingSlices);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteBool(this._isSpawned);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteBool(this._hasPedestal);
		}
		if ((base.syncVarDirtyBits & 8UL) != 0UL)
		{
			writer.WriteVector3(this._position);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this._remainingSlices, null, reader.ReadByte());
			base.GeneratedSyncVarDeserialize<bool>(ref this._isSpawned, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize<bool>(ref this._hasPedestal, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, new Action<Vector3, Vector3>(this.SetPosition), reader.ReadVector3());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<byte>(ref this._remainingSlices, null, reader.ReadByte());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this._isSpawned, null, reader.ReadBool());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this._hasPedestal, null, reader.ReadBool());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, new Action<Vector3, Vector3>(this.SetPosition), reader.ReadVector3());
		}
	}

	private const float SpawnChance = 33f;

	public static readonly int ShaderDissolveProperty = Shader.PropertyToID("_Dissolve");

	private static readonly CachedLayerMask CheckerMask = new CachedLayerMask(new string[] { "Pickup", "Hitbox" });

	private static readonly Vector3 PedestalOffset = new Vector3(0f, -1.042f, 0f);

	private static readonly List<Vector4> PossiblePositions = new List<Vector4>();

	private static readonly Dictionary<RoomIdentifier, int> PopulatedRooms = new Dictionary<RoomIdentifier, int>();

	private static readonly Dictionary<RoomName, Vector4> Spawnpoints;

	[SerializeField]
	private GameObject _visuals;

	[SerializeField]
	private GameObject _pedestal;

	[SerializeField]
	private Material _dissolveMat;

	[SerializeField]
	private GameObject[] _slices;

	[SerializeField]
	private float _dissolveSpeed;

	[SerializeField]
	private AudioClip _useSound;

	[SerializeField]
	private AudioClip _appearSound;

	[SerializeField]
	private AudioClip _disappearClip;

	[SerializeField]
	private byte _maxSlices;

	[SyncVar]
	private byte _remainingSlices;

	[SyncVar]
	private bool _isSpawned;

	[SyncVar]
	private bool _hasPedestal;

	[SerializeField]
	private float _spawnedDuration;

	[SerializeField]
	private float _respawnTime;

	[SerializeField]
	private float _cakeRadius;

	[SyncVar(hook = "SetPosition")]
	private Vector3 _position;

	private float _fade;

	private float _remainingTime;

	private int _prevSlices = -1;
}
