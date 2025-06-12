using System.Collections.Generic;
using UnityEngine;

namespace Interactables.Interobjects;

public class ElevatorPanel : InteractableCollider
{
	public static readonly List<ElevatorPanel> AllPanels = new List<ElevatorPanel>();

	[SerializeField]
	private Material[] _levelMats;

	[SerializeField]
	private Material _movingUpMat;

	[SerializeField]
	private Material _movingDownMat;

	[SerializeField]
	private Material _disabledMat;

	[SerializeField]
	private Renderer _targetRenderer;

	[SerializeField]
	private ElevatorDoor _door;

	private bool _isInternal;

	private bool _chamberFound;

	public ElevatorChamber AssignedChamber { get; private set; }

	protected override void Awake()
	{
		ElevatorPanel.AllPanels.Add(this);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		ElevatorDoor.OnLocksChanged -= RefreshPanelConditionally;
		ElevatorPanel.AllPanels.Remove(this);
	}

	private void Update()
	{
		if (!this._chamberFound)
		{
			if (this._door == null)
			{
				this.FindChamberInternal();
			}
			else
			{
				this.FindChamberRegular();
			}
		}
	}

	private void FindChamberRegular()
	{
		if (ElevatorChamber.TryGetChamber(this._door.Group, out var chamber))
		{
			base.ColliderId = this.GetHashFromHeight(base.transform.position.y);
			this.AssignChamber(chamber);
		}
	}

	private void FindChamberInternal()
	{
		if (!base.transform.TryGetComponentInParent<ElevatorChamber>(out var comp))
		{
			Debug.LogError("This panel has chamber assigned despite being an internal panel.", base.gameObject);
		}
		this._isInternal = true;
		this.AssignChamber(comp);
	}

	private void AssignChamber(ElevatorChamber chamber)
	{
		if (this._isInternal)
		{
			base.Target = chamber;
		}
		else
		{
			this._door.Chamber = chamber;
			base.Target = this._door;
		}
		this.AssignedChamber = chamber;
		this._chamberFound = true;
		base.Awake();
		this.RefreshPanel();
		chamber.OnSequenceChanged += RefreshPanel;
		ElevatorDoor.OnLocksChanged += RefreshPanelConditionally;
	}

	private void RefreshPanelConditionally(ElevatorGroup group)
	{
		if (this._chamberFound && this.AssignedChamber.AssignedGroup == group)
		{
			this.RefreshPanel();
		}
	}

	private void RefreshPanel()
	{
		ElevatorChamber.ElevatorSequence curSequence = this.AssignedChamber.CurSequence;
		if ((uint)(curSequence - 4) <= 1u)
		{
			this.RefreshStationary();
		}
		else
		{
			this.RefreshMoving();
		}
	}

	private void RefreshStationary()
	{
		bool flag = (this._isInternal ? this.AssignedChamber.DestinationDoor : this._door).ActiveLocks != 0;
		int destinationLevel = this.AssignedChamber.DestinationLevel;
		this._targetRenderer.sharedMaterial = (flag ? this._disabledMat : this._levelMats[destinationLevel]);
	}

	private void RefreshMoving()
	{
		this._targetRenderer.sharedMaterial = (this.AssignedChamber.GoingUp ? this._movingUpMat : this._movingDownMat);
	}

	private byte GetHashFromHeight(float y)
	{
		return (byte)(Mathf.RoundToInt(Mathf.Abs(y + 200f) * 0.3f) + 1);
	}
}
