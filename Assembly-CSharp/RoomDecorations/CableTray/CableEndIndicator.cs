using System.Collections.Generic;
using MapGeneration;
using UnityEngine;

namespace RoomDecorations.CableTray;

public class CableEndIndicator : MonoBehaviour
{
	public static readonly HashSet<CableEndIndicator> UnconnectedInstances = new HashSet<CableEndIndicator>();

	public static readonly HashSet<CableEndIndicator> AllInstances = new HashSet<CableEndIndicator>();

	private const float BorderTolerance = 0.2f;

	private const float ConnectionTolerance = 0.16f;

	private bool _isUnconnected;

	public Vector3 Position { get; private set; }

	[field: SerializeField]
	public bool OpenEnded { get; private set; }

	private void Awake()
	{
		this.Position = base.transform.position;
		CableEndIndicator.AllInstances.Add(this);
		if (!this.IsRoomBorder())
		{
			return;
		}
		foreach (CableEndIndicator unconnectedInstance in CableEndIndicator.UnconnectedInstances)
		{
			if (!((this.Position - unconnectedInstance.Position).sqrMagnitude > 0.16f))
			{
				unconnectedInstance.RemoveFromUnconnected();
				return;
			}
		}
		this._isUnconnected = true;
		CableEndIndicator.UnconnectedInstances.Add(this);
	}

	private void OnDestroy()
	{
		this.RemoveFromUnconnected();
		CableEndIndicator.AllInstances.Remove(this);
	}

	private void RemoveFromUnconnected()
	{
		if (this._isUnconnected)
		{
			this._isUnconnected = false;
			CableEndIndicator.UnconnectedInstances.Remove(this);
		}
	}

	private bool IsRoomBorder()
	{
		Vector3 fwd = this.Position + Vector3.forward * 0.2f;
		if (this.CompareCoords(fwd, Vector3.back) && this.CompareCoords(fwd, Vector3.left))
		{
			return !this.CompareCoords(fwd, Vector3.right);
		}
		return true;
	}

	private bool CompareCoords(Vector3 fwd, Vector3 dir)
	{
		return fwd.CompareCoords(this.Position + dir * 0.2f);
	}
}
