using System;
using MapGeneration.Holidays;
using Mirror;
using UnityEngine;

namespace MapGeneration
{
	public class SpawnableRoom : MonoBehaviour
	{
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
			if (this._netIdCacheSet)
			{
				return;
			}
			NetworkIdentity[] componentsInChildren = base.GetComponentsInChildren<NetworkIdentity>(true);
			this.OriginalIdentities = new SpawnableRoom.OriginalNetIdentity[componentsInChildren.Length];
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				this.OriginalIdentities[i] = new SpawnableRoom.OriginalNetIdentity
				{
					AssetId = (uint)((ulong)SpawnableRoom._nextAssetId + (ulong)((long)i)),
					Target = componentsInChildren[i].gameObject
				};
			}
			int num = Mathf.Clamp(this.MaxAmount, 1, 12);
			SpawnableRoom._nextAssetId += (uint)(num * componentsInChildren.Length);
			this._netIdCacheSet = true;
		}

		public void SetupNetIdHandlers(int prevSpawnedCnt)
		{
			this.DuplicateId = prevSpawnedCnt;
			int num = this.OriginalIdentities.Length;
			this._assetIdOffset = (uint)(num * prevSpawnedCnt);
			for (int i = 0; i < num; i++)
			{
				SpawnableRoom.OriginalNetIdentity originalNetIdentity = this.OriginalIdentities[i];
				uint num2 = originalNetIdentity.AssetId + this._assetIdOffset;
				if (NetworkServer.active)
				{
					NetworkServer.Spawn(originalNetIdentity.Target, num2, null);
				}
				else
				{
					originalNetIdentity.Target.SetActive(false);
					originalNetIdentity.Target.hideFlags = HideFlags.NotEditable;
					NetworkClient.RegisterSpawnHandler(num2, new SpawnHandlerDelegate(this.SpawnNetIdentity), new UnSpawnDelegate(global::UnityEngine.Object.Destroy));
				}
			}
		}

		private GameObject SpawnNetIdentity(SpawnMessage spawnMessage)
		{
			foreach (SpawnableRoom.OriginalNetIdentity originalNetIdentity in this.OriginalIdentities)
			{
				if (originalNetIdentity.AssetId + this._assetIdOffset == spawnMessage.assetId)
				{
					originalNetIdentity.Target.SetActive(true);
					originalNetIdentity.Target.hideFlags = HideFlags.None;
					return originalNetIdentity.Target;
				}
			}
			throw new InvalidOperationException("Failed to spawn sub-identity with asset ID " + spawnMessage.assetId.ToString());
		}

		private RoomIdentifier _roomIdentifier;

		private bool _roomIdentifierSet;

		private bool _netIdCacheSet;

		private uint _assetIdOffset;

		private const int MaxRegistrations = 12;

		private static uint _nextAssetId = 1U;

		public int MinAmount;

		public int MaxAmount;

		public float ChanceMultiplier = 1f;

		public float AdjacentChanceMultiplier = 0.1f;

		public float FirstChanceMultiplier = 100f;

		public bool SpecialRoom;

		[HideInInspector]
		public SpawnableRoom.OriginalNetIdentity[] OriginalIdentities;

		public HolidayRoomVariant[] HolidayVariants;

		[Serializable]
		public struct OriginalNetIdentity
		{
			public GameObject Target;

			public uint AssetId;
		}
	}
}
