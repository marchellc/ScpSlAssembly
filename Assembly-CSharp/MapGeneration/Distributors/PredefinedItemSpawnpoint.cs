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
		_isSelected = true;
	}

	public override bool TryGeneratePickup(out ItemPickupBase pickup)
	{
		if (!_isSelected && _scenarioDefined)
		{
			pickup = null;
			return false;
		}
		Transform parentRoom = PossibleSpawnpoints.RandomItem();
		pickup = ItemDistributor.ServerCreatePickup(TargetItem, parentRoom);
		return pickup != null;
	}

	protected override void Awake()
	{
		base.Awake();
		_scenarioDefined = Scenario != null;
		if (_scenarioDefined)
		{
			Scenario.Register(this);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (_scenarioDefined)
		{
			Scenario.Unregister(this);
		}
	}
}
