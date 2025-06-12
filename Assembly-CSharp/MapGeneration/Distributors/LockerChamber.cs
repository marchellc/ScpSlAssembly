using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors;

public class LockerChamber : MonoBehaviour, IDoorPermissionRequester
{
	public ItemType[] AcceptableItems;

	public bool IsOpen;

	public DoorPermissionFlags RequiredPermissions;

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

	[SerializeField]
	private KeycardScannerNfcIcon _nfcScanner;

	[SerializeField]
	private KeycardScannerPermsIndicator[] _permsIndicators;

	private static readonly int DoorHash = Animator.StringToHash("isOpen");

	private static readonly int DeniedHash = Animator.StringToHash("accessDenied");

	private static readonly int GrantedHash = Animator.StringToHash("accessGranted");

	private const float MinimalTimeSinceMapGeneration = 5f;

	private readonly Stopwatch _stopwatch = new Stopwatch();

	private byte _animatorStatusCode;

	private bool _prevDoor;

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

	public bool AnimatorSet
	{
		get
		{
			if (this._animatorStatusCode == 0)
			{
				this._animatorStatusCode = (byte)((this._animator == null) ? 1u : 2u);
			}
			return this._animatorStatusCode == 2;
		}
	}

	public DoorPermissionsPolicy PermissionsPolicy => new DoorPermissionsPolicy(this.RequiredPermissions, requireAll: true);

	[field: SerializeField]
	public string RequesterLogSignature { get; set; }

	public event Action OnDoorStatusSet;

	protected virtual void Start()
	{
		KeycardScannerPermsIndicator[] permsIndicators = this._permsIndicators;
		for (int i = 0; i < permsIndicators.Length; i++)
		{
			permsIndicators[i].Register(this);
		}
	}

	public virtual void SpawnItem(ItemType id, int amount)
	{
		if (id == ItemType.None || !InventoryItemLoader.AvailableItems.TryGetValue(id, out var value))
		{
			return;
		}
		for (int i = 0; i < amount; i++)
		{
			this.GetSpawnpoint(id, i, out var worldPosition, out var worldRotation, out var parent);
			ItemPickupBase itemPickupBase = UnityEngine.Object.Instantiate(value.PickupDropModel, worldPosition, worldRotation);
			itemPickupBase.transform.SetParent(parent);
			itemPickupBase.NetworkInfo = new PickupSyncInfo(id, value.Weight, 0, locked: true);
			this.Content.Add(itemPickupBase);
			(itemPickupBase as IPickupDistributorTrigger)?.OnDistributed();
			if (itemPickupBase.TryGetComponent<Rigidbody>(out var component))
			{
				component.isKinematic = true;
				component.transform.ResetLocalPose();
				SpawnablesDistributorBase.BodiesToUnfreeze.Add(component);
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
		if (doorStatus != this._prevDoor)
		{
			this.IsOpen = doorStatus;
			this._prevDoor = doorStatus;
			if (this.AnimatorSet)
			{
				this._animator.SetBool(LockerChamber.DoorHash, doorStatus);
				this.TargetCooldown = this._animationTime;
				this._stopwatch.Restart();
			}
			if (this._nfcScanner != null)
			{
				this._nfcScanner.SetTemporaryGranted(this._animationTime);
			}
			KeycardScannerPermsIndicator[] permsIndicators = this._permsIndicators;
			for (int i = 0; i < permsIndicators.Length; i++)
			{
				permsIndicators[i].PlayAccepted(this._animationTime);
			}
			if (this.IsOpen && !this.WasEverOpened)
			{
				this.OnFirstTimeOpen();
				this.WasEverOpened = true;
			}
			this.OnDoorStatusSet?.Invoke();
		}
	}

	public void PlayDenied(AudioClip deniedClip, DoorPermissionFlags flags, float cooldown)
	{
	}

	protected virtual void OnFirstTimeOpen()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (ItemPickupBase item in this.Content)
		{
			if (!(item == null))
			{
				PickupSyncInfo info = item.Info;
				info.Locked = false;
				item.NetworkInfo = info;
			}
		}
		if (!this.SpawnOnFirstChamberOpening)
		{
			return;
		}
		foreach (ItemPickupBase item2 in this.ToBeSpawned)
		{
			if (!(item2 == null))
			{
				ItemDistributor.SpawnPickup(item2);
			}
		}
	}
}
