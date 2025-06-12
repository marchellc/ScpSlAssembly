using System;
using MapGeneration.Holidays;
using Mirror;
using UnityEngine;

namespace MapGeneration;

public class SpawnableRoom : MonoBehaviour
{
	[Serializable]
	public struct OriginalNetIdentity
	{
		public GameObject Target;

		public uint AssetId;
	}

	private RoomIdentifier _roomIdentifier;

	private bool _roomIdentifierSet;

	private bool _netIdCacheSet;

	private uint _assetIdOffset;

	private const int MaxRegistrations = 12;

	private static uint _nextAssetId = 1u;

	public int MinAmount;

	public int MaxAmount;

	public float ChanceMultiplier = 1f;

	public float AdjacentChanceMultiplier = 0.1f;

	public float FirstChanceMultiplier = 100f;

	public bool SpecialRoom;

	[HideInInspector]
	public OriginalNetIdentity[] OriginalIdentities;

	public HolidayRoomVariant[] HolidayVariants;

	public int DuplicateId { get; private set; }

	public RoomIdentifier Room
	{
		get
		{
			if (!this._roomIdentifierSet)
			{
				this._roomIdentifier = base.GetComponent<RoomIdentifier>();
				this._roomIdentifierSet = true;
			}
			return this._roomIdentifier;
		}
	}

	public void RegisterIdentities()
	{
		if (!this._netIdCacheSet)
		{
			NetworkIdentity[] componentsInChildren = base.GetComponentsInChildren<NetworkIdentity>(includeInactive: true);
			this.OriginalIdentities = new OriginalNetIdentity[componentsInChildren.Length];
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				this.OriginalIdentities[i] = new OriginalNetIdentity
				{
					AssetId = (uint)(SpawnableRoom._nextAssetId + i),
					Target = componentsInChildren[i].gameObject
				};
			}
			int num = Mathf.Clamp(this.MaxAmount, 1, 12);
			SpawnableRoom._nextAssetId += (uint)(num * componentsInChildren.Length);
			this._netIdCacheSet = true;
		}
	}

	public void SetupNetIdHandlers(int prevSpawnedCnt)
	{
		this.DuplicateId = prevSpawnedCnt;
		int num = this.OriginalIdentities.Length;
		this._assetIdOffset = (uint)(num * prevSpawnedCnt);
		for (int i = 0; i < num; i++)
		{
			OriginalNetIdentity originalNetIdentity = this.OriginalIdentities[i];
			uint assetId = originalNetIdentity.AssetId + this._assetIdOffset;
			if (NetworkServer.active)
			{
				NetworkServer.Spawn(originalNetIdentity.Target, assetId);
				continue;
			}
			originalNetIdentity.Target.SetActive(value: false);
			originalNetIdentity.Target.hideFlags = HideFlags.NotEditable;
			NetworkClient.RegisterSpawnHandler(assetId, SpawnNetIdentity, UnityEngine.Object.Destroy);
		}
	}

	private GameObject SpawnNetIdentity(SpawnMessage spawnMessage)
	{
		OriginalNetIdentity[] originalIdentities = this.OriginalIdentities;
		for (int i = 0; i < originalIdentities.Length; i++)
		{
			OriginalNetIdentity originalNetIdentity = originalIdentities[i];
			if (originalNetIdentity.AssetId + this._assetIdOffset == spawnMessage.assetId)
			{
				originalNetIdentity.Target.SetActive(value: true);
				originalNetIdentity.Target.hideFlags = HideFlags.None;
				return originalNetIdentity.Target;
			}
		}
		throw new InvalidOperationException("Failed to spawn sub-identity with asset ID " + spawnMessage.assetId);
	}
}
