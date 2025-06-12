using InventorySystem.Items.Pickups;
using MapGeneration.Scenarios;
using UnityEngine;

namespace MapGeneration.Distributors;

public class PredefinedItemSpawnpoint : ItemSpawnpointBase, IScenarioProcessor
{
	public ItemType TargetItem;

	public Transform[] PossibleSpawnpoints;

	public DistributorScenario Scenario;

	private bool _isSelected;

	private bool _scenarioDefined;

	public void Select()
	{
		this._isSelected = true;
	}

	public override bool TryGeneratePickup(out ItemPickupBase pickup)
	{
		if (!this._isSelected && this._scenarioDefined)
		{
			pickup = null;
			return false;
		}
		Transform parentRoom = this.PossibleSpawnpoints.RandomItem();
		pickup = ItemDistributor.ServerCreatePickup(this.TargetItem, parentRoom);
		return pickup != null;
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
