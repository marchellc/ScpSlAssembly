using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp106;

public static class Scp106PocketItemManager
{
	public class PocketItem
	{
		public double TriggerTime;

		public bool Remove;

		public bool WarningSent;

		public RelativePosition DropPosition;
	}

	public struct WarningMessage : NetworkMessage
	{
		public RelativePosition Position;
	}

	public static readonly float[] RecycleChances = new float[3] { 0.5f, 0.7f, 1f };

	public static Vector2 TimerRange = new Vector2(90f, 240f);

	public static readonly Dictionary<ItemPickupBase, PocketItem> TrackedItems = new Dictionary<ItemPickupBase, PocketItem>();

	private const float WarningTime = 3f;

	private const float RaycastRange = 30f;

	private const float SoundRange = 12f;

	private const float SpawnOffset = 0.3f;

	private const float RandomEscapeVelocity = 0.2f;

	private const int MaxValidPositions = 64;

	private static readonly Vector3[] ValidPositionsNonAlloc = new Vector3[64];

	private static readonly HashSet<ItemPickupBase> ToRemove = new HashSet<ItemPickupBase>();

	private static float RandomVel => UnityEngine.Random.Range(-0.2f, 0.2f);

	public static event Action<ItemPickupBase, PocketItem> OnPocketItemAdded;

	public static event Action<ItemPickupBase> OnPocketItemRemoved;

	public static void AddItem(ItemPickupBase itemPickup)
	{
		if (InventoryItemLoader.TryGetItem<ItemBase>(itemPickup.Info.ItemId, out var result))
		{
			PocketItem pocketItem = new PocketItem
			{
				Remove = (UnityEngine.Random.value > Scp106PocketItemManager.RecycleChances[Scp106PocketItemManager.GetRarity(result)]),
				TriggerTime = NetworkTime.time + (double)UnityEngine.Random.Range(Scp106PocketItemManager.TimerRange.x, Scp106PocketItemManager.TimerRange.y),
				DropPosition = Scp106PocketItemManager.GetRandomValidSpawnPosition(),
				WarningSent = false
			};
			Scp106PocketItemManager.OnPocketItemAdded?.Invoke(itemPickup, pocketItem);
			Scp106PocketItemManager.TrackedItems.Add(itemPickup, pocketItem);
		}
	}

	public static void RemoveItem(ItemPickupBase itemPickup)
	{
		Scp106PocketItemManager.OnPocketItemRemoved?.Invoke(itemPickup);
		Scp106PocketItemManager.TrackedItems.Remove(itemPickup);
	}

	public static int GetRarity(ItemBase ib)
	{
		int num = 0;
		if (Scp106PocketItemManager.HasFlagFast(ib, ItemTierFlags.Rare))
		{
			num++;
		}
		if (Scp106PocketItemManager.HasFlagFast(ib, ItemTierFlags.MilitaryGrade))
		{
			num++;
		}
		if (Scp106PocketItemManager.HasFlagFast(ib, ItemTierFlags.ExtraRare))
		{
			num += 2;
		}
		return Mathf.Min(num, Scp106PocketItemManager.RecycleChances.Length - 1);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ItemPickupBase.OnPickupAdded += OnAdded;
		ItemPickupBase.OnPickupDestroyed += OnRemoved;
		StaticUnityMethods.OnUpdate += Update;
		CustomNetworkManager.OnClientReady += delegate
		{
			Scp106PocketItemManager.TrackedItems.Clear();
			NetworkClient.ReplaceHandler(delegate(WarningMessage x)
			{
				if (PlayerRoleLoader.TryGetRoleTemplate<Scp106Role>(RoleTypeId.Scp106, out var result))
				{
					AudioSourcePoolManager.PlayAtPosition(result.ItemSpawnSound, x.Position, 12f);
				}
			});
		};
	}

	private static void Update()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = false;
		foreach (KeyValuePair<ItemPickupBase, PocketItem> trackedItem in Scp106PocketItemManager.TrackedItems)
		{
			if (!trackedItem.Key || !Scp106PocketItemManager.IsInPocketDimension(trackedItem.Key.transform.position))
			{
				flag |= Scp106PocketItemManager.ToRemove.Add(trackedItem.Key);
				continue;
			}
			PocketItem value = trackedItem.Value;
			double num = value.TriggerTime - NetworkTime.time;
			if (num > 3.0)
			{
				continue;
			}
			if (!value.Remove && !value.WarningSent)
			{
				NetworkServer.SendToAll(new WarningMessage
				{
					Position = value.DropPosition
				}, 0, sendToReadyOnly: true);
				value.WarningSent = true;
			}
			if (!(num > 0.0))
			{
				ItemPickupBase key = trackedItem.Key;
				Rigidbody component;
				if (value.Remove)
				{
					key.DestroySelf();
				}
				else if (key.TryGetComponent<Rigidbody>(out component))
				{
					component.linearVelocity = new Vector3(Scp106PocketItemManager.RandomVel, Physics.gravity.y, Scp106PocketItemManager.RandomVel);
					key.transform.position = value.DropPosition.Position;
				}
				value.TriggerTime = NetworkTime.time + (double)UnityEngine.Random.Range(Scp106PocketItemManager.TimerRange.x, Scp106PocketItemManager.TimerRange.y);
			}
		}
		if (flag)
		{
			Scp106PocketItemManager.ToRemove.ForEach(delegate(ItemPickupBase x)
			{
				Scp106PocketItemManager.RemoveItem(x);
			});
		}
	}

	private static void OnAdded(ItemPickupBase ipb)
	{
		if (NetworkServer.active && Scp106PocketItemManager.IsInPocketDimension(ipb.transform.position))
		{
			Scp106PocketItemManager.AddItem(ipb);
		}
	}

	private static void OnRemoved(ItemPickupBase ipb)
	{
		if (NetworkServer.active)
		{
			Scp106PocketItemManager.TrackedItems.Remove(ipb);
		}
	}

	private static bool IsInPocketDimension(Vector3 position)
	{
		if (position.TryGetRoom(out var room))
		{
			return room.Name == RoomName.Pocket;
		}
		return false;
	}

	private static RelativePosition GetRandomValidSpawnPosition()
	{
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!(allHub.roleManager.CurrentRole is IFpcRole fpcRole))
			{
				continue;
			}
			Vector3 position = fpcRole.FpcModule.Position;
			if (!Scp106PocketItemManager.IsInPocketDimension(position) && Scp106PocketItemManager.TryGetRoofPosition(position, out var result))
			{
				Scp106PocketItemManager.ValidPositionsNonAlloc[num] = result;
				if (++num > 64)
				{
					break;
				}
			}
		}
		if (num > 0)
		{
			return new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[UnityEngine.Random.Range(0, num)]);
		}
		foreach (RoomIdentifier allRoomIdentifier in RoomIdentifier.AllRoomIdentifiers)
		{
			if ((allRoomIdentifier.Zone == FacilityZone.HeavyContainment || allRoomIdentifier.Zone == FacilityZone.Entrance) && Scp106PocketItemManager.TryGetRoofPosition(allRoomIdentifier.transform.position, out var result2) && !Scp106PocketItemManager.IsInPocketDimension(result2))
			{
				Scp106PocketItemManager.ValidPositionsNonAlloc[num] = result2;
				if (++num > 64)
				{
					break;
				}
			}
		}
		if (num == 0)
		{
			throw new InvalidOperationException("GetRandomValidSpawnPosition found no valid spawn positions.");
		}
		int num2 = UnityEngine.Random.Range(0, num);
		return new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[num2]);
	}

	private static bool TryGetRoofPosition(Vector3 point, out Vector3 result)
	{
		if (Physics.Raycast(point, Vector3.up, out var hitInfo, 30f, FpcStateProcessor.Mask))
		{
			result = hitInfo.point + Vector3.down * 0.3f;
			return true;
		}
		result = Vector3.zero;
		return false;
	}

	private static bool HasFlagFast(ItemBase ib, ItemTierFlags flag)
	{
		return (ib.TierFlags & flag) == flag;
	}
}
