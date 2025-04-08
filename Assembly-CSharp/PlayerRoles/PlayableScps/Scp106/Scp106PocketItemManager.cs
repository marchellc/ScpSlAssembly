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

namespace PlayerRoles.PlayableScps.Scp106
{
	public static class Scp106PocketItemManager
	{
		public static event Action<ItemPickupBase, Scp106PocketItemManager.PocketItem> OnPocketItemAdded;

		public static event Action<ItemPickupBase> OnPocketItemRemoved;

		private static float RandomVel
		{
			get
			{
				return global::UnityEngine.Random.Range(-0.2f, 0.2f);
			}
		}

		public static void AddItem(ItemPickupBase itemPickup)
		{
			ItemBase itemBase;
			if (!InventoryItemLoader.TryGetItem<ItemBase>(itemPickup.Info.ItemId, out itemBase))
			{
				return;
			}
			Scp106PocketItemManager.PocketItem pocketItem = new Scp106PocketItemManager.PocketItem
			{
				Remove = (global::UnityEngine.Random.value > Scp106PocketItemManager.RecycleChances[Scp106PocketItemManager.GetRarity(itemBase)]),
				TriggerTime = NetworkTime.time + (double)global::UnityEngine.Random.Range(Scp106PocketItemManager.TimerRange.x, Scp106PocketItemManager.TimerRange.y),
				DropPosition = Scp106PocketItemManager.GetRandomValidSpawnPosition(),
				WarningSent = false
			};
			Action<ItemPickupBase, Scp106PocketItemManager.PocketItem> onPocketItemAdded = Scp106PocketItemManager.OnPocketItemAdded;
			if (onPocketItemAdded != null)
			{
				onPocketItemAdded(itemPickup, pocketItem);
			}
			Scp106PocketItemManager.TrackedItems.Add(itemPickup, pocketItem);
		}

		public static void RemoveItem(ItemPickupBase itemPickup)
		{
			Action<ItemPickupBase> onPocketItemRemoved = Scp106PocketItemManager.OnPocketItemRemoved;
			if (onPocketItemRemoved != null)
			{
				onPocketItemRemoved(itemPickup);
			}
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
			ItemPickupBase.OnPickupAdded += Scp106PocketItemManager.OnAdded;
			ItemPickupBase.OnPickupDestroyed += Scp106PocketItemManager.OnRemoved;
			StaticUnityMethods.OnUpdate += Scp106PocketItemManager.Update;
			CustomNetworkManager.OnClientReady += delegate
			{
				Scp106PocketItemManager.TrackedItems.Clear();
				NetworkClient.ReplaceHandler<Scp106PocketItemManager.WarningMessage>(delegate(Scp106PocketItemManager.WarningMessage x)
				{
					Scp106Role scp106Role;
					if (!PlayerRoleLoader.TryGetRoleTemplate<Scp106Role>(RoleTypeId.Scp106, out scp106Role))
					{
						return;
					}
					AudioSourcePoolManager.PlayAtPosition(scp106Role.ItemSpawnSound, x.Position, 12f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
				}, true);
			};
		}

		private static void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			bool flag = false;
			foreach (KeyValuePair<ItemPickupBase, Scp106PocketItemManager.PocketItem> keyValuePair in Scp106PocketItemManager.TrackedItems)
			{
				if (keyValuePair.Key == null || !Scp106PocketItemManager.ValidateHeight(keyValuePair.Key))
				{
					flag |= Scp106PocketItemManager.ToRemove.Add(keyValuePair.Key);
				}
				else
				{
					Scp106PocketItemManager.PocketItem value = keyValuePair.Value;
					double num = value.TriggerTime - NetworkTime.time;
					if (num <= 3.0)
					{
						if (!value.Remove && !value.WarningSent)
						{
							NetworkServer.SendToAll<Scp106PocketItemManager.WarningMessage>(new Scp106PocketItemManager.WarningMessage
							{
								Position = value.DropPosition
							}, 0, true);
							value.WarningSent = true;
						}
						if (num <= 0.0)
						{
							ItemPickupBase key = keyValuePair.Key;
							Rigidbody rigidbody;
							if (value.Remove)
							{
								key.DestroySelf();
							}
							else if (key.TryGetComponent<Rigidbody>(out rigidbody))
							{
								rigidbody.velocity = new Vector3(Scp106PocketItemManager.RandomVel, Physics.gravity.y, Scp106PocketItemManager.RandomVel);
								key.transform.position = value.DropPosition.Position;
							}
							flag |= Scp106PocketItemManager.ToRemove.Add(key);
						}
					}
				}
			}
			if (!flag)
			{
				return;
			}
			Scp106PocketItemManager.ToRemove.ForEach(delegate(ItemPickupBase x)
			{
				Scp106PocketItemManager.RemoveItem(x);
			});
		}

		private static void OnAdded(ItemPickupBase ipb)
		{
			if (!NetworkServer.active || !Scp106PocketItemManager.ValidateHeight(ipb))
			{
				return;
			}
			Scp106PocketItemManager.AddItem(ipb);
		}

		private static void OnRemoved(ItemPickupBase ipb)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp106PocketItemManager.TrackedItems.Remove(ipb);
		}

		private static bool ValidateHeight(ItemPickupBase ipb)
		{
			float y = ipb.transform.position.y;
			return y >= Scp106PocketItemManager.HeightLimit.y && y <= Scp106PocketItemManager.HeightLimit.x;
		}

		private static RelativePosition GetRandomValidSpawnPosition()
		{
			int num = 0;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					Vector3 position = fpcRole.FpcModule.Position;
					Vector3 vector;
					if (position.y >= Scp106PocketItemManager.HeightLimit.x && Scp106PocketItemManager.TryGetRoofPosition(position, out vector))
					{
						Scp106PocketItemManager.ValidPositionsNonAlloc[num] = vector;
						if (++num > 64)
						{
							break;
						}
					}
				}
			}
			if (num > 0)
			{
				return new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[global::UnityEngine.Random.Range(0, num)]);
			}
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				Vector3 vector2;
				if ((roomIdentifier.Zone == FacilityZone.HeavyContainment || roomIdentifier.Zone == FacilityZone.Entrance) && Scp106PocketItemManager.TryGetRoofPosition(roomIdentifier.transform.position, out vector2))
				{
					Scp106PocketItemManager.ValidPositionsNonAlloc[num] = vector2;
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
			int num2 = global::UnityEngine.Random.Range(0, num);
			return new RelativePosition(Scp106PocketItemManager.ValidPositionsNonAlloc[num2]);
		}

		private static bool TryGetRoofPosition(Vector3 point, out Vector3 result)
		{
			RaycastHit raycastHit;
			if (Physics.Raycast(point, Vector3.up, out raycastHit, 30f, FpcStateProcessor.Mask))
			{
				result = raycastHit.point + Vector3.down * 0.3f;
				return true;
			}
			result = Vector3.zero;
			return false;
		}

		private static bool HasFlagFast(ItemBase ib, ItemTierFlags flag)
		{
			return (ib.TierFlags & flag) == flag;
		}

		public static readonly float[] RecycleChances = new float[] { 0.5f, 0.7f, 1f };

		public static Vector2 TimerRange = new Vector2(90f, 240f);

		public static readonly Dictionary<ItemPickupBase, Scp106PocketItemManager.PocketItem> TrackedItems = new Dictionary<ItemPickupBase, Scp106PocketItemManager.PocketItem>();

		private const float WarningTime = 3f;

		private const float RaycastRange = 30f;

		private const float SoundRange = 12f;

		private const float SpawnOffset = 0.3f;

		private const float RandomEscapeVelocity = 0.2f;

		private const int MaxValidPositions = 64;

		private static readonly Vector3[] ValidPositionsNonAlloc = new Vector3[64];

		private static readonly HashSet<ItemPickupBase> ToRemove = new HashSet<ItemPickupBase>();

		private static readonly Vector2 HeightLimit = new Vector2(-1990f, -2002f);

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
	}
}
