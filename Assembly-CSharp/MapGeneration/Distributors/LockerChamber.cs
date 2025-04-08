using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class LockerChamber : MonoBehaviour
	{
		public event Action OnDoorStatusSet;

		public bool CanInteract
		{
			get
			{
				if (!this.AnimatorSet)
				{
					return false;
				}
				if (!this._stopwatch.IsRunning)
				{
					return true;
				}
				if (this._stopwatch.Elapsed.TotalSeconds >= (double)this.TargetCooldown)
				{
					this._stopwatch.Stop();
					return true;
				}
				return false;
			}
		}

		public virtual void SpawnItem(ItemType id, int amount)
		{
			ItemBase itemBase;
			if (id == ItemType.None || !InventoryItemLoader.AvailableItems.TryGetValue(id, out itemBase))
			{
				return;
			}
			for (int i = 0; i < amount; i++)
			{
				Vector3 vector;
				Quaternion quaternion;
				Transform transform;
				this.GetSpawnpoint(id, i, out vector, out quaternion, out transform);
				ItemPickupBase itemPickupBase = global::UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, vector, quaternion);
				itemPickupBase.transform.SetParent(transform);
				itemPickupBase.NetworkInfo = new PickupSyncInfo(id, itemBase.Weight, 0, true);
				this.Content.Add(itemPickupBase);
				IPickupDistributorTrigger pickupDistributorTrigger = itemPickupBase as IPickupDistributorTrigger;
				if (pickupDistributorTrigger != null)
				{
					pickupDistributorTrigger.OnDistributed();
				}
				Rigidbody rigidbody;
				if (itemPickupBase.TryGetComponent<Rigidbody>(out rigidbody))
				{
					rigidbody.isKinematic = true;
					rigidbody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					SpawnablesDistributorBase.BodiesToUnfreeze.Add(rigidbody);
				}
				if (this.SpawnOnFirstChamberOpening)
				{
					this.ToBeSpawned.Add(itemPickupBase);
				}
				else
				{
					ItemDistributor.SpawnPickup(itemPickupBase);
				}
			}
		}

		protected virtual void GetSpawnpoint(ItemType itemType, int index, out Vector3 worldPosition, out Quaternion worldRotation, out Transform parent)
		{
			Transform transform;
			if (this._useMultipleSpawnpoints && this._spawnpoints.Length != 0)
			{
				int num = index % this._spawnpoints.Length;
				transform = this._spawnpoints[num];
			}
			else
			{
				transform = this.Spawnpoint;
			}
			parent = transform;
			worldPosition = transform.position;
			worldRotation = transform.rotation;
		}

		public void SetDoor(bool doorStatus, AudioClip beepClip)
		{
			if (doorStatus == this._prevDoor)
			{
				return;
			}
			this.IsOpen = doorStatus;
			this._prevDoor = doorStatus;
			if (this.AnimatorSet)
			{
				this._animator.SetBool(LockerChamber.DoorHash, doorStatus);
				this.TargetCooldown = this._animationTime;
				this._stopwatch.Restart();
			}
			if (!NetworkServer.active)
			{
				Action onDoorStatusSet = this.OnDoorStatusSet;
				if (onDoorStatusSet == null)
				{
					return;
				}
				onDoorStatusSet();
				return;
			}
			else
			{
				if (doorStatus && !this.WasEverOpened)
				{
					this.WasEverOpened = true;
					foreach (ItemPickupBase itemPickupBase in this.Content)
					{
						if (!(itemPickupBase == null))
						{
							PickupSyncInfo info = itemPickupBase.Info;
							info.Locked = false;
							itemPickupBase.NetworkInfo = info;
						}
					}
					if (!this.SpawnOnFirstChamberOpening)
					{
						return;
					}
					foreach (ItemPickupBase itemPickupBase2 in this.ToBeSpawned)
					{
						if (!(itemPickupBase2 == null))
						{
							ItemDistributor.SpawnPickup(itemPickupBase2);
						}
					}
				}
				Action onDoorStatusSet2 = this.OnDoorStatusSet;
				if (onDoorStatusSet2 == null)
				{
					return;
				}
				onDoorStatusSet2();
				return;
			}
		}

		public void PlayDenied(AudioClip deniedClip)
		{
			this.TargetCooldown = 0.4f;
			this._stopwatch.Restart();
		}

		public bool AnimatorSet
		{
			get
			{
				if (this._animatorStatusCode == 0)
				{
					this._animatorStatusCode = ((this._animator == null) ? 1 : 2);
				}
				return this._animatorStatusCode == 2;
			}
		}

		public ItemType[] AcceptableItems;

		public bool IsOpen;

		public KeycardPermissions RequiredPermissions;

		public readonly List<ItemPickupBase> Content = new List<ItemPickupBase>();

		public Transform Spawnpoint;

		public bool SpawnOnFirstChamberOpening;

		[NonSerialized]
		public readonly List<ItemPickupBase> ToBeSpawned = new List<ItemPickupBase>();

		[NonSerialized]
		public float TargetCooldown;

		[NonSerialized]
		public bool WasEverOpened;

		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private bool _useMultipleSpawnpoints;

		[SerializeField]
		private Transform[] _spawnpoints;

		[SerializeField]
		private float _animationTime = 1f;

		private static readonly int DoorHash = Animator.StringToHash("isOpen");

		private static readonly int DeniedHash = Animator.StringToHash("accessDenied");

		private static readonly int GrantedHash = Animator.StringToHash("accessGranted");

		private const float MinimalTimeSinceMapGeneration = 5f;

		private readonly Stopwatch _stopwatch = new Stopwatch();

		private byte _animatorStatusCode;

		private bool _prevDoor;
	}
}
