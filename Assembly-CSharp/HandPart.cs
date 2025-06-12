using GameObjectPools;
using InventorySystem.Items;
using InventorySystem.Items.Thirdperson;
using UnityEngine;

public class HandPart : ThirdpersonItemBase
{
	public GameObject TargetPart;

	public ItemType TargetItemId;

	public bool UseUniversalAnimations;

	[SerializeField]
	private GameObject optionalPrefab;

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
			this.SpawnedObject = (PoolManager.Singleton.TryGetPoolObject(this.optionalPrefab, this.TargetPart.transform, out var poolObject) ? poolObject.gameObject : Object.Instantiate(this.optionalPrefab, this.TargetPart.transform));
			this.SpawnedObject.transform.localScale = Vector3.one;
			this.SpawnedObject.transform.localPosition = Vector3.zero;
			this.SpawnedObject.transform.localRotation = Quaternion.identity;
			this.OnActiveStateChange(isEnabled: true);
		}
		else if (this.SpawnedObject != null)
		{
			this.OnActiveStateChange(isEnabled: false);
			if (!PoolManager.Singleton.TryReturnPoolObject(this.SpawnedObject))
			{
				Object.Destroy(this.SpawnedObject);
			}
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.TargetPart.SetActive(value: false);
		this.CurrentlyEnabled = false;
		this.OnActiveStateChange(isEnabled: false);
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
		return new ThirdpersonLayerWeight(1f, allowOther: false);
	}
}
