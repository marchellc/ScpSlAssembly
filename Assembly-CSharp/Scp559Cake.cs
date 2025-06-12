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
	private const float SpawnChance = 33f;

	public static readonly int ShaderDissolveProperty = Shader.PropertyToID("_Dissolve");

	private static readonly CachedLayerMask CheckerMask = new CachedLayerMask("Pickup", "Hitbox");

	private static readonly Vector3 PedestalOffset = new Vector3(0f, -1.042f, 0f);

	private static readonly List<Vector4> PossiblePositions = new List<Vector4>();

	private static readonly Dictionary<RoomIdentifier, int> PopulatedRooms = new Dictionary<RoomIdentifier, int>();

	private static readonly Dictionary<RoomName, Vector4> Spawnpoints = new Dictionary<RoomName, Vector4>
	{
		[RoomName.LczClassDSpawn] = new Vector4(-19f, 0f, 0f, 1f),
		[RoomName.LczToilets] = new Vector4(-5.3f, 0.95f, -6.5f, 0f),
		[RoomName.LczGreenhouse] = new Vector4(0f, 0f, -5.5f, 1f),
		[RoomName.LczComputerRoom] = new Vector4(6f, 0f, 4.5f, 1f),
		[RoomName.LczGlassroom] = new Vector4(8.3f, 1.05f, -5.9f, 0f),
		[RoomName.LczArmory] = new Vector4(4.14f, 0.82f, -0.95f, 0f),
		[RoomName.Lcz330] = new Vector4(2f, 0.91f, 0.7f, 0f),
		[RoomName.Lcz173] = new Vector4(-2.56f, 12.32f, -4.67f, 0f),
		[RoomName.Lcz914] = new Vector4(1.1f, 1.011f, -7.16f, 0f),
		[RoomName.LczAirlock] = new Vector4(0.65f, 0f, -4.6f, 1f),
		[RoomName.HczServers] = new Vector4(2f, 0f, -0.35f, 1f),
		[RoomName.HczWarhead] = new Vector4(0.65f, 291.89f, 10.51f, 0f),
		[RoomName.HczArmory] = new Vector4(0.89f, 0.88f, -1.44f, 0f),
		[RoomName.HczMicroHID] = new Vector4(2.3f, 0.92f, -5.27f, 0f),
		[RoomName.Hcz049] = new Vector4(-5.65f, 192.44f, -1.75f, 1f),
		[RoomName.Hcz079] = new Vector4(1.8f, -3.33f, -6.3f, 1f),
		[RoomName.Hcz096] = new Vector4(-4.4f, 0f, 1f, 1f),
		[RoomName.Hcz106] = new Vector4(24.38f, 0.86f, -15.54f, 0f),
		[RoomName.Hcz939] = new Vector4(0.53f, 1.05f, 2.9f, 0f),
		[RoomName.HczTestroom] = new Vector4(0.915f, 0.74f, -4.7f, 0f),
		[RoomName.HczCheckpointToEntranceZone] = new Vector4(-6.07f, 0f, -3.35f, 1f),
		[RoomName.EzOfficeStoried] = new Vector4(-2.26f, 0.89f, 0.4f, 0f),
		[RoomName.EzOfficeLarge] = new Vector4(1.14f, 0.89f, 0.29f, 0f),
		[RoomName.EzOfficeSmall] = new Vector4(6f, -0.54f, 2.7f, 0f),
		[RoomName.EzIntercom] = new Vector4(-6.9f, -5.04f, -2.8f, 0f),
		[RoomName.EzGateA] = new Vector4(2.9f, 0.91f, 1.19f, 0f),
		[RoomName.EzGateB] = new Vector4(3.38f, 0.91f, -2.8f, 0f)
	};

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

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public bool CanBeUsed => this._remainingSlices > 0;

	public byte Network_remainingSlices
	{
		get
		{
			return this._remainingSlices;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._remainingSlices, 1uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._isSpawned, 2uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._hasPedestal, 4uL, null);
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
			base.GeneratedSyncVarSetter(value, ref this._position, 8uL, SetPosition);
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (this._isSpawned && ply.IsHuman() && this._remainingSlices != 0)
		{
			Scp559Effect effect = ply.playerEffectsController.GetEffect<Scp559Effect>();
			if (!effect.IsEnabled)
			{
				this.Network_remainingSlices = (byte)(this._remainingSlices - 1);
				effect.IsEnabled = true;
			}
		}
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
		}
		else
		{
			this.SetPosition(this._position, this._position);
		}
	}

	private void SetPosition(Vector3 prev, Vector3 cur)
	{
		base.transform.position = cur;
	}

	private void UpdateServer()
	{
		this._remainingTime -= Time.deltaTime;
		if (!(this._remainingTime > 0f))
		{
			Vector3 pos;
			bool pedestal;
			if (this._isSpawned)
			{
				this.Network_isSpawned = false;
				this._remainingTime = this._respawnTime;
			}
			else if (RoundStart.RoundStarted && !(RoundStart.RoundLength.TotalSeconds < (double)this._respawnTime) && this.TryGetSpawnPoint(out pos, out pedestal))
			{
				this.Network_position = pos;
				this.Network_isSpawned = true;
				this.Network_hasPedestal = pedestal;
				this.Network_remainingSlices = this._maxSlices;
				this._remainingTime = this._spawnedDuration;
			}
		}
	}

	private void UpdateVisual()
	{
		this._pedestal.SetActive(this._hasPedestal);
		float num = Mathf.Clamp01(this._fade + this._dissolveSpeed * Time.deltaTime * (float)(this._isSpawned ? 1 : (-1)));
		if (num != this._fade)
		{
			if (this._fade <= 0f)
			{
				AudioSourcePoolManager.PlayOnTransform(this._appearSound, base.transform, 25f);
			}
			if (this._fade >= 1f)
			{
				AudioSourcePoolManager.PlayOnTransform(this._disappearClip, base.transform, 15f);
			}
			this._fade = num;
			this._dissolveMat.SetFloat(Scp559Cake.ShaderDissolveProperty, 1f - this._fade);
			this._visuals.SetActive(this._fade > 0f);
			this._prevSlices = -1;
			this._slices.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
		}
		else if (this._prevSlices != this._remainingSlices)
		{
			for (int num2 = 0; num2 < this._slices.Length; num2++)
			{
				this._slices[num2].SetActive(num2 < this._remainingSlices);
			}
			if (this._prevSlices > this._remainingSlices)
			{
				AudioSourcePoolManager.PlayOnTransform(this._useSound, base.transform, 9f);
			}
			this._prevSlices = this._remainingSlices;
		}
	}

	private bool TryGetSpawnPoint(out Vector3 pos, out bool pedestal)
	{
		Scp559Cake.PossiblePositions.Clear();
		Scp559Cake.PopulatedRooms.Clear();
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is HumanRole && allHub.TryGetCurrentRoom(out var room) && Scp559Cake.Spawnpoints.ContainsKey(room.Name))
			{
				Scp559Cake.PopulatedRooms.TryGetValue(room, out var value);
				if (!allHub.playerEffectsController.GetEffect<Scp559Effect>().IsEnabled)
				{
					value++;
				}
				Scp559Cake.PopulatedRooms[room] = value;
				num = Mathf.Max(value, num);
			}
		}
		foreach (KeyValuePair<RoomIdentifier, int> populatedRoom in Scp559Cake.PopulatedRooms)
		{
			if (populatedRoom.Value >= num)
			{
				Vector4 vector = Scp559Cake.Spawnpoints[populatedRoom.Key.Name];
				Vector3 position = populatedRoom.Key.transform.TransformPoint(vector);
				if (!Physics.CheckSphere(position, this._cakeRadius, Scp559Cake.CheckerMask))
				{
					Scp559Cake.PossiblePositions.Add(new Vector4(position.x, position.y, position.z, vector.w));
				}
			}
		}
		if (Scp559Cake.PossiblePositions.Count == 0)
		{
			pos = Vector3.zero;
			pedestal = false;
			return false;
		}
		Vector4 vector2 = Scp559Cake.PossiblePositions.RandomItem();
		pos = vector2;
		pedestal = vector2.w > 0f;
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
		System.Random random = new System.Random(SeedSynchronizer.Seed);
		return 33.0 >= random.NextDouble() * 100.0;
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
			NetworkWriterExtensions.WriteByte(writer, this._remainingSlices);
			writer.WriteBool(this._isSpawned);
			writer.WriteBool(this._hasPedestal);
			writer.WriteVector3(this._position);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._remainingSlices);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteBool(this._isSpawned);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteBool(this._hasPedestal);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteVector3(this._position);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._remainingSlices, null, NetworkReaderExtensions.ReadByte(reader));
			base.GeneratedSyncVarDeserialize(ref this._isSpawned, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._hasPedestal, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._position, SetPosition, reader.ReadVector3());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._remainingSlices, null, NetworkReaderExtensions.ReadByte(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._isSpawned, null, reader.ReadBool());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._hasPedestal, null, reader.ReadBool());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._position, SetPosition, reader.ReadVector3());
		}
	}
}
