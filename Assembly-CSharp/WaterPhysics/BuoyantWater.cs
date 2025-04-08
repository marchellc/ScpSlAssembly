using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace WaterPhysics
{
	public class BuoyantWater : MonoBehaviour
	{
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

		private Vector3 FlowDirection
		{
			get
			{
				return this.FastTr.rotation * this._localFlowDirection;
			}
		}

		private void FixedUpdate()
		{
			for (int i = this._tracked.Count - 1; i >= 0; i--)
			{
				BuoyantWater.TrackedRigidbody trackedRigidbody = this._tracked[i];
				if (trackedRigidbody.Rigidbody == null)
				{
					this._tracked.RemoveAt(i);
				}
				else
				{
					double num = NetworkTime.time - trackedRigidbody.EnterTime;
					Vector3 position = trackedRigidbody.Rigidbody.position;
					Vector3 vector = this.EvaluateFlowForceAtPoint(position) * this.FlowDirection;
					float num2 = position.y - this.FastTr.position.y;
					float num3 = Mathf.InverseLerp(this._startHeight, this._fullSubmergeWeight, num2);
					BuoyancyProcessor.ProcessFixedUpdate(trackedRigidbody.Rigidbody, trackedRigidbody.Mode, (float)num, num3, vector);
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
			this._tracked.Add(new BuoyantWater.TrackedRigidbody
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
			for (int i = this._tracked.Count - 1; i >= 0; i--)
			{
				BuoyantWater.TrackedRigidbody trackedRigidbody = this._tracked[i];
				if (!(trackedRigidbody.Rigidbody != attachedRigidbody))
				{
					BuoyancyProcessor.ProcessExit(trackedRigidbody.Rigidbody);
					this._tracked.RemoveAt(i);
					return;
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
			BuoyancyDefinition buoyancyDefinition;
			if (rb.TryGetComponent<BuoyancyDefinition>(out buoyancyDefinition))
			{
				return buoyancyDefinition.Mode;
			}
			ItemPickupBase itemPickupBase;
			if (!rb.TryGetComponent<ItemPickupBase>(out itemPickupBase))
			{
				return BuoyancyMode.LongTimeFloaters;
			}
			ItemBase itemBase;
			if (!itemPickupBase.Info.ItemId.TryGetTemplate(out itemBase))
			{
				return BuoyancyMode.Floater;
			}
			BuoyancyMode buoyancyMode;
			if (!BuoyantWater.BuoyancyByPickupCategory.TryGetValue(itemBase.Category, out buoyancyMode))
			{
				return BuoyancyMode.Floater;
			}
			return buoyancyMode;
		}

		private float EvaluateFlowForceAtPoint(Vector3 point)
		{
			float num = Vector3.Dot(point - this.FastTr.position, this.FlowDirection);
			return this._forceOverDot.Evaluate(num);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static BuoyantWater()
		{
			Dictionary<ItemCategory, BuoyancyMode> dictionary = new Dictionary<ItemCategory, BuoyancyMode>();
			dictionary[ItemCategory.Ammo] = BuoyancyMode.ShortTimeFloaters;
			dictionary[ItemCategory.Armor] = BuoyancyMode.ShortTimeFloaters;
			dictionary[ItemCategory.Firearm] = BuoyancyMode.NonBuoyant;
			dictionary[ItemCategory.Grenade] = BuoyancyMode.NonBuoyant;
			dictionary[ItemCategory.Radio] = BuoyancyMode.NonBuoyant;
			dictionary[ItemCategory.SpecialWeapon] = BuoyancyMode.SuperHeavy;
			BuoyantWater.BuoyancyByPickupCategory = dictionary;
		}

		private static readonly Dictionary<ItemCategory, BuoyancyMode> BuoyancyByPickupCategory;

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

		private readonly List<BuoyantWater.TrackedRigidbody> _tracked = new List<BuoyantWater.TrackedRigidbody>();

		private struct TrackedRigidbody
		{
			public Rigidbody Rigidbody;

			public BuoyancyMode Mode;

			public double EnterTime;
		}
	}
}
