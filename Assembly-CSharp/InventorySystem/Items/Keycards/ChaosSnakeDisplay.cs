using InventorySystem.Items.Keycards.Snake;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class ChaosSnakeDisplay : MonoBehaviour
{
	[SerializeField]
	private SnakeDisplay _display;

	[SerializeField]
	private bool _checkForActiveInspect;

	private IIdentifierProvider _idProvider;

	private bool _firstTime;

	private void Awake()
	{
		this._firstTime = true;
		this._idProvider = base.GetComponentInParent<IIdentifierProvider>();
	}

	private void Update()
	{
		if (this._idProvider == null)
		{
			return;
		}
		ushort serialNumber = this._idProvider.ItemId.SerialNumber;
		if (this._checkForActiveInspect)
		{
			double value;
			bool flag = KeycardItem.StartInspectTimes.TryGetValue(serialNumber, out value) && NetworkTime.time - value > 1.7000000476837158;
			this._display.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
		}
		if (ChaosKeycardItem.SnakeSessions.TryGetValue(serialNumber, out var value2))
		{
			value2.UpdateDisplay(this._display, this._firstTime);
			this._firstTime = false;
		}
	}
}
