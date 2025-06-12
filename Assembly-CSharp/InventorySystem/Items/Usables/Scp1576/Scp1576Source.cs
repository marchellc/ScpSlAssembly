using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Source : MonoBehaviour
{
	public static Action<Scp1576Source> OnRemoved;

	public static HashSet<Scp1576Source> Instances = new HashSet<Scp1576Source>();

	private Transform _cachedTransform;

	private bool _transformCacheSet;

	private bool _positionUpToDate;

	private Vector3 _lastPos;

	public Vector3 Position
	{
		get
		{
			if (!this._positionUpToDate)
			{
				this._lastPos = this.CachedTransform.position;
				this._positionUpToDate = true;
			}
			return this._lastPos;
		}
	}

	[field: SerializeField]
	public bool HideGlobalIndicator { get; private set; }

	private Transform CachedTransform
	{
		get
		{
			if (!this._transformCacheSet)
			{
				this._cachedTransform = base.transform;
				this._transformCacheSet = true;
			}
			return this._cachedTransform;
		}
	}

	private void Update()
	{
		this._positionUpToDate = false;
	}

	private void OnEnable()
	{
		Scp1576Source.Instances.Add(this);
	}

	private void OnDisable()
	{
		Scp1576Source.OnRemoved?.Invoke(this);
		Scp1576Source.Instances.Remove(this);
	}

	public override int GetHashCode()
	{
		return base.gameObject.GetHashCode();
	}
}
