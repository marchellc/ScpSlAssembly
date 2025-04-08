using System;
using GameObjectPools;
using InventorySystem.Items;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

public class HandPart : ThirdpersonItemBase
{
	protected bool CurrentlyEnabled { get; set; }

	protected GameObject SpawnedObject { get; set; }

	public void UpdateItem()
	{
		bool flag = base.OwnerHub.inventory.CurItem.TypeId == this.TargetItemId;
		if (flag == this.CurrentlyEnabled)
		{
			return;
		}
		this.CurrentlyEnabled = flag;
		this.TargetPart.SetActive(flag);
		if (flag && this.optionalPrefab != null)
		{
			PoolObject poolObject;
			this.SpawnedObject = (PoolManager.Singleton.TryGetPoolObject(this.optionalPrefab, this.TargetPart.transform, out poolObject, true) ? poolObject.gameObject : global::UnityEngine.Object.Instantiate<GameObject>(this.optionalPrefab, this.TargetPart.transform));
			this.SpawnedObject.transform.localScale = Vector3.one;
			this.SpawnedObject.transform.localPosition = Vector3.zero;
			this.SpawnedObject.transform.localRotation = Quaternion.identity;
			this.OnActiveStateChange(true);
			return;
		}
		if (this.SpawnedObject != null)
		{
			this.OnActiveStateChange(false);
			if (!PoolManager.Singleton.TryReturnPoolObject(this.SpawnedObject))
			{
				global::UnityEngine.Object.Destroy(this.SpawnedObject);
			}
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.TargetPart.SetActive(false);
		this.CurrentlyEnabled = false;
		this.OnActiveStateChange(false);
	}

	protected virtual void OnActiveStateChange(bool isEnabled)
	{
	}

	public override float GetTransitionTime(ItemIdentifier iid)
	{
		return 2f;
	}

	public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
	{
		return new ThirdpersonLayerWeight(1f, false);
	}

	public GameObject TargetPart;

	public ItemType TargetItemId;

	public bool UseUniversalAnimations;

	[SerializeField]
	private GameObject optionalPrefab;
}
