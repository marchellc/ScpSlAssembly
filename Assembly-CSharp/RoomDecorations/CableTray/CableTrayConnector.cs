using System.Collections.Generic;
using Interactables.Interobjects;
using MapGeneration;
using UnityEngine;

namespace RoomDecorations.CableTray;

public class CableTrayConnector : MonoBehaviour
{
	[SerializeField]
	private GameObject _seal;

	[SerializeField]
	private GameObject _passthrough;

	[SerializeField]
	private bool _destroyOpenEnded;

	private IRoomConnector _roomConnector;

	private const float DetectionHorizontalRangeSqr = 0.2f;

	private const float DetectionVerticalRangeAbs = 10f;

	private void Awake()
	{
		this._roomConnector = base.GetComponent<IRoomConnector>();
		if (this._roomConnector.RoomsAlreadyRegistered)
		{
			this.FindConnections();
		}
		else
		{
			this._roomConnector.OnRoomsRegistered += FindConnections;
		}
	}

	private void FindConnections()
	{
		CableEndIndicator result2;
		if (this.TryFindCableEnd(CableEndIndicator.UnconnectedInstances, out var result))
		{
			if (result.OpenEnded && this._destroyOpenEnded)
			{
				result.gameObject.SetActive(value: false);
				this.SetStatus(passthrough: false, seal: false);
			}
			else
			{
				this.SetStatus(passthrough: false, seal: true);
				Vector3 v = result.GetComponentInParent<RoomIdentifier>().transform.position - base.transform.position;
				this._seal.transform.SetPositionAndRotation(result.transform.position, Quaternion.LookRotation(v.NormalizeIgnoreY()));
			}
		}
		else if (this.TryFindCableEnd(CableEndIndicator.AllInstances, out result2))
		{
			this.SetStatus(passthrough: true, seal: false);
		}
		else
		{
			this.SetStatus(passthrough: false, seal: false);
		}
	}

	private void SetStatus(bool passthrough, bool seal)
	{
		if (this._seal != null)
		{
			this._seal.SetActive(seal);
		}
		if (this._passthrough != null)
		{
			this._passthrough.SetActive(passthrough);
		}
	}

	private bool TryFindCableEnd(HashSet<CableEndIndicator> src, out CableEndIndicator result)
	{
		Vector3 position = base.transform.position;
		foreach (CableEndIndicator item in src)
		{
			Vector3 v = item.Position - position;
			if (!(v.SqrMagnitudeIgnoreY() > 0.2f) && !(v.MagnitudeOnlyY() > 10f))
			{
				result = item;
				return true;
			}
		}
		result = null;
		return false;
	}
}
