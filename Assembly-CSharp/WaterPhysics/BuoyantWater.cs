using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace WaterPhysics;

public class BuoyantWater : MonoBehaviour
{
	private struct TrackedRigidbody
	{
		public Rigidbody Rigidbody;

		public BuoyancyMode Mode;

		public double EnterTime;
	}

	private static readonly Dictionary<ItemCategory, BuoyancyMode> BuoyancyByPickupCategory = new Dictionary<ItemCategory, BuoyancyMode>
	{
		[ItemCategory.Ammo] = BuoyancyMode.ShortTimeFloaters,
		[ItemCategory.Armor] = BuoyancyMode.ShortTimeFloaters,
		[ItemCategory.Firearm] = BuoyancyMode.NonBuoyant,
		[ItemCategory.Grenade] = BuoyancyMode.NonBuoyant,
		[ItemCategory.Radio] = BuoyancyMode.NonBuoyant,
		[ItemCategory.SpecialWeapon] = BuoyancyMode.SuperHeavy
	};

	private const BuoyancyMode DefaultBuoyancyPickup = BuoyancyMode.Floater;

	private const BuoyancyMode DefaultBuoyancyOther = BuoyancyMode.LongTimeFloaters;

	[SerializeField]
	private float _startHeight;

	[SerializeField]
	private float _fullSubmergeWeight;

	[SerializeField]
	private Vector3 _localFlowDirection;

	[SerializeField]
	private AnimationCurve _forceOverDot;

	private bool _trCacheSet;

	private Transform _cachedTransform;

	private readonly List<TrackedRigidbody> _tracked = new List<TrackedRigidbody>();

	private Transform FastTr
	{
		get
		{
			if (!this._trCacheSet)
			{
				this._trCacheSet = true;
				this._cachedTransform = base.transform;
			}
			return this._cachedTransform;
		}
	}

	private Vector3 FlowDirection => this.FastTr.rotation * this._localFlowDirection;

	private void FixedUpdate()
	{
		for (int num = this._tracked.Count - 1; num >= 0; num--)
		{
			TrackedRigidbody trackedRigidbody = this._tracked[num];
			if (trackedRigidbody.Rigidbody == null)
			{
				this._tracked.RemoveAt(num);
			}
			else
			{
				double num2 = NetworkTime.time - trackedRigidbody.EnterTime;
				Vector3 position = trackedRigidbody.Rigidbody.position;
				Vector3 flow = this.EvaluateFlowForceAtPoint(position) * this.FlowDirection;
				float value = position.y - this.FastTr.position.y;
				float submergeRatio = Mathf.InverseLerp(this._startHeight, this._fullSubmergeWeight, value);
				BuoyancyProcessor.ProcessFixedUpdate(trackedRigidbody.Rigidbody, trackedRigidbody.Mode, (float)num2, submergeRatio, flow);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (attachedRigidbody == null)
		{
			return;
		}
		for (int i = 0; i < this._tracked.Count; i++)
		{
			if (!(this._tracked[i].Rigidbody != attachedRigidbody))
			{
				return;
			}
		}
		BuoyancyMode modeForRigidbody = this.GetModeForRigidbody(attachedRigidbody);
		this._tracked.Add(new TrackedRigidbody
		{
			EnterTime = NetworkTime.time,
			Mode = modeForRigidbody,
			Rigidbody = attachedRigidbody
		});
		BuoyancyProcessor.ProcessNew(attachedRigidbody, modeForRigidbody);
	}

	private void OnTriggerExit(Collider other)
	{
		Rigidbody attachedRigidbody = other.attachedRigidbody;
		if (attachedRigidbody == null)
		{
			return;
		}
		for (int num = this._tracked.Count - 1; num >= 0; num--)
		{
			TrackedRigidbody trackedRigidbody = this._tracked[num];
			if (!(trackedRigidbody.Rigidbody != attachedRigidbody))
			{
				BuoyancyProcessor.ProcessExit(trackedRigidbody.Rigidbody);
				this._tracked.RemoveAt(num);
				break;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Vector3 position = this.FastTr.position;
		Gizmos.DrawLine(position + Vector3.up * this._fullSubmergeWeight, position + Vector3.up * this._startHeight);
		Vector3 flowDirection = this.FlowDirection;
		for (int i = 0; i < this._forceOverDot.length; i++)
		{
			Keyframe keyframe = this._forceOverDot[i];
			Gizmos.DrawSphere(position + flowDirection * keyframe.time, keyframe.value / 5f + 0.05f);
		}
	}

	private BuoyancyMode GetModeForRigidbody(Rigidbody rb)
	{
		if (rb.TryGetComponent<BuoyancyDefinition>(out var component))
		{
			return component.Mode;
		}
		if (!rb.TryGetComponent<ItemPickupBase>(out var component2))
		{
			return BuoyancyMode.LongTimeFloaters;
		}
		if (!component2.Info.ItemId.TryGetTemplate<ItemBase>(out var item))
		{
			return BuoyancyMode.Floater;
		}
		if (!BuoyantWater.BuoyancyByPickupCategory.TryGetValue(item.Category, out var value))
		{
			return BuoyancyMode.Floater;
		}
		return value;
	}

	private float EvaluateFlowForceAtPoint(Vector3 point)
	{
		float time = Vector3.Dot(point - this.FastTr.position, this.FlowDirection);
		return this._forceOverDot.Evaluate(time);
	}
}
