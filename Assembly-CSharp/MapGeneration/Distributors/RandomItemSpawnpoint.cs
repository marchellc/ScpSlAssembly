using System;
using InventorySystem.Items.Pickups;
using MapGeneration.Scenarios;
using UnityEngine;

namespace MapGeneration.Distributors;

public class RandomItemSpawnpoint : ItemSpawnpointBase, IScenarioProcessor
{
	[Serializable]
	public struct ItemPreset
	{
		public ItemType TargetItem;

		public Transform[] PossibleSpawnpoints;

		[Range(0f, 100f)]
		public float Chance;
	}

	public ItemPreset[] Presets;

	public DistributorScenario Scenario;

	private bool _isSelected;

	private bool _scenarioDefined;

	public float TotalWeight
	{
		get
		{
			float num = 0f;
			ItemPreset[] presets = this.Presets;
			for (int i = 0; i < presets.Length; i++)
			{
				ItemPreset itemPreset = presets[i];
				num += itemPreset.Chance;
			}
			return num;
		}
	}

	public void Select()
	{
		this._isSelected = true;
	}

	public override bool TryGeneratePickup(out ItemPickupBase pickup)
	{
		ItemPreset? randomItemPreset = this.GetRandomItemPreset();
		bool flag = !this._isSelected && this._scenarioDefined;
		if (!randomItemPreset.HasValue || flag)
		{
			pickup = null;
			return false;
		}
		Transform parentRoom = randomItemPreset.Value.PossibleSpawnpoints.RandomItem();
		pickup = ItemDistributor.ServerCreatePickup(randomItemPreset.Value.TargetItem, parentRoom);
		return pickup != null;
	}

	public ItemPreset? GetRandomItemPreset()
	{
		if (this.Presets == null || this.Presets.Length == 0)
		{
			return null;
		}
		float num = UnityEngine.Random.Range(0f, this.TotalWeight);
		float num2 = 0f;
		ItemPreset[] presets = this.Presets;
		for (int i = 0; i < presets.Length; i++)
		{
			ItemPreset value = presets[i];
			num2 += value.Chance;
			if (num <= num2)
			{
				return value;
			}
		}
		return null;
	}

	protected override void Awake()
	{
		base.Awake();
		this._scenarioDefined = this.Scenario != null;
		if (this._scenarioDefined)
		{
			this.Scenario.Register(this);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (this._scenarioDefined)
		{
			this.Scenario.Unregister(this);
		}
	}
}
