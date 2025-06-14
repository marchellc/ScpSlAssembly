using System.Collections.Generic;
using Footprinting;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class DisruptorWorldmodelActionExtension : MonoBehaviour, IWorldmodelExtension, IPickupLockingExtension
{
	private static readonly int DissolveHash = Shader.PropertyToID("_Dissolve");

	private static readonly Dictionary<Material, Queue<Material>> DissolveMatPool = new Dictionary<Material, Queue<Material>>();

	private static readonly List<Material> MatsNonAlloc = new List<Material>();

	private FirearmWorldmodel _worldmodel;

	private float[] _scheduledRemainingShotTimes;

	private Footprint _scheduledAttackerFootprint;

	private DisruptorActionModule.FiringState _scheduledFiringState;

	private Rigidbody _rigidbody;

	private float _breakingProgress;

	private bool _isWorldmodel;

	private Dictionary<Material, Material> _materialInstances;

	[SerializeField]
	private float _singleShotForce;

	[SerializeField]
	private float _rapidFireForce;

	[SerializeField]
	private float _torqueMultiplier;

	[SerializeField]
	private float _breakingSpeed;

	[SerializeField]
	private float _breakingDestroyThreshold;

	[SerializeField]
	private MeshRenderer[] _renderers;

	private bool Breaking => ParticleDisruptor.BrokenSerials.Contains(this._worldmodel.Identifier.SerialNumber);

	public bool LockPrefab
	{
		get
		{
			if (this._scheduledRemainingShotTimes == null)
			{
				return this.Breaking;
			}
			return true;
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.ServerUpdateShots();
		}
		if (this._worldmodel.WorldmodelType == FirearmWorldmodelType.Pickup && this.Breaking)
		{
			this.UpdateBreaking();
		}
	}

	private void Start()
	{
		this._rigidbody = base.GetComponentInParent<Rigidbody>();
	}

	private void OnDestroy()
	{
		if (this._materialInstances == null)
		{
			return;
		}
		foreach (KeyValuePair<Material, Material> materialInstance in this._materialInstances)
		{
			DisruptorWorldmodelActionExtension.DissolveMatPool.GetOrAdd(materialInstance.Key, () => new Queue<Material>()).Enqueue(materialInstance.Value);
		}
	}

	private Material DuplicateMaterial(Material shared)
	{
		if (DisruptorWorldmodelActionExtension.DissolveMatPool.TryGetValue(shared, out var value))
		{
			return value.Dequeue();
		}
		return new Material(shared);
	}

	private void ReplaceMaterials(MeshRenderer rend)
	{
		DisruptorWorldmodelActionExtension.MatsNonAlloc.Clear();
		rend.GetSharedMaterials(DisruptorWorldmodelActionExtension.MatsNonAlloc);
		int count = DisruptorWorldmodelActionExtension.MatsNonAlloc.Count;
		Material[] array = new Material[count];
		for (int i = 0; i < count; i++)
		{
			Material shared = DisruptorWorldmodelActionExtension.MatsNonAlloc[i];
			array[i] = this._materialInstances.GetOrAdd(shared, () => this.DuplicateMaterial(shared));
		}
		rend.sharedMaterials = array;
	}

	private void UpdateBreaking()
	{
		if (this._materialInstances == null)
		{
			this._materialInstances = new Dictionary<Material, Material>();
			this._renderers.ForEach(ReplaceMaterials);
		}
		this._breakingProgress += Time.deltaTime * this._breakingSpeed;
		foreach (KeyValuePair<Material, Material> materialInstance in this._materialInstances)
		{
			materialInstance.Value.SetFloat(DisruptorWorldmodelActionExtension.DissolveHash, this._breakingProgress);
		}
		if (this._breakingProgress > this._breakingDestroyThreshold && NetworkServer.active)
		{
			ItemPickupBase componentInParent = base.GetComponentInParent<ItemPickupBase>();
			if (componentInParent != null)
			{
				componentInParent.DestroySelf();
			}
			else
			{
				base.enabled = false;
			}
		}
	}

	private void ServerUpdateShots()
	{
		if (this._scheduledRemainingShotTimes == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < this._scheduledRemainingShotTimes.Length; i++)
		{
			float num = this._scheduledRemainingShotTimes[i];
			if (!(num < 0f))
			{
				num -= Time.deltaTime;
				if (num > 0f)
				{
					flag = true;
				}
				else
				{
					this.ServerFire();
				}
				this._scheduledRemainingShotTimes[i] = num;
			}
		}
		if (!flag)
		{
			this._scheduledRemainingShotTimes = null;
			ItemIdentifier identifier = this._worldmodel.Identifier;
			ParticleDisruptor template = identifier.TypeId.GetTemplate<ParticleDisruptor>();
			if (template.TryGetModule<MagazineModule>(out var module) && module.GetAmmoStoredForSerial(identifier.SerialNumber) <= 0)
			{
				template.ServerSendBrokenRpc(identifier.SerialNumber);
			}
		}
	}

	private void ServerFire()
	{
		if (this._worldmodel.TryGetExtension<BarrelTipExtension>(out var extension))
		{
			DisruptorHitregModule.TemplateSimulateShot(new DisruptorShotEvent(this._worldmodel.Identifier, this._scheduledAttackerFootprint, this._scheduledFiringState), extension);
			if (this._rigidbody != null)
			{
				float num = ((this._scheduledFiringState == DisruptorActionModule.FiringState.FiringSingle) ? this._singleShotForce : this._rapidFireForce);
				this._rigidbody.AddForceAtPosition(-extension.WorldspaceDirection * num, extension.WorldspacePosition, ForceMode.Impulse);
				this._rigidbody.AddRelativeTorque(this._torqueMultiplier * num * Vector3.right, ForceMode.Impulse);
			}
		}
	}

	public void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		this._worldmodel = worldmodel;
	}

	public void ServerScheduleShot(Footprint attackerFootprint, DisruptorActionModule.FiringState firingState, float[] remainingShots)
	{
		this._scheduledAttackerFootprint = attackerFootprint;
		this._scheduledFiringState = firingState;
		this._scheduledRemainingShotTimes = remainingShots;
	}
}
