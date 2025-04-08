using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public static class HitregUtils
	{
		public static bool ServerDealDamage(this IDestructible target, FiringModeControllerModule firingCtrl, float damage)
		{
			MicroHidDamageHandler microHidDamageHandler = new MicroHidDamageHandler(firingCtrl, damage);
			if (!target.Damage(damage, microHidDamageHandler, target.CenterOfMass))
			{
				return false;
			}
			HitboxIdentity hitboxIdentity = target as HitboxIdentity;
			if (hitboxIdentity != null)
			{
				Hitmarker.SendHitmarkerConditionally(1f, microHidDamageHandler, hitboxIdentity.TargetHub);
			}
			else
			{
				Hitmarker.SendHitmarkerDirectly(firingCtrl.MicroHid.Owner, 1f, true);
			}
			return true;
		}

		public static void Raycast(Transform plyCam, float thickness, float range, out int detections)
		{
			Vector3 position = plyCam.position;
			detections = Physics.SphereCastNonAlloc(position, thickness, plyCam.forward, HitregUtils.HitsNonAlloc, range, HitregUtils.DetectionMask);
			HitregUtils.DetectedDestructibles.Clear();
			HitregUtils.DetectedNetIds.Clear();
			for (int i = 0; i < detections; i++)
			{
				Collider collider = HitregUtils.HitsNonAlloc[i].collider;
				HitregUtils.DetectionsNonAlloc[i] = collider;
				IDestructible destructible;
				RaycastHit raycastHit;
				if (collider.TryGetComponent<IDestructible>(out destructible) && (!Physics.Linecast(position, destructible.CenterOfMass, out raycastHit, PlayerRolesUtils.BlockerMask) || !(raycastHit.collider != collider)) && HitregUtils.DetectedNetIds.Add(destructible.NetworkId))
				{
					HitregUtils.DetectedDestructibles.Add(destructible);
				}
			}
		}

		public static void OverlapSphere(Vector3 point, float radius, out int detections, Predicate<IDestructible> prevalidator = null)
		{
			detections = Physics.OverlapSphereNonAlloc(point, radius, HitregUtils.DetectionsNonAlloc, HitregUtils.DetectionMask);
			HitregUtils.DetectedDestructibles.Clear();
			HitregUtils.DetectedNetIds.Clear();
			for (int i = 0; i < detections; i++)
			{
				IDestructible destructible;
				if (HitregUtils.DetectionsNonAlloc[i].TryGetComponent<IDestructible>(out destructible) && !Physics.Linecast(point, destructible.CenterOfMass, PlayerRolesUtils.BlockerMask) && (prevalidator == null || prevalidator(destructible)) && HitregUtils.DetectedNetIds.Add(destructible.NetworkId))
				{
					HitregUtils.DetectedDestructibles.Add(destructible);
				}
			}
		}

		public static readonly CachedLayerMask DetectionMask = new CachedLayerMask(new string[] { "Hitbox", "Glass", "Door" });

		public static readonly Collider[] DetectionsNonAlloc = new Collider[64];

		public static readonly RaycastHit[] HitsNonAlloc = new RaycastHit[64];

		public static readonly HashSet<uint> DetectedNetIds = new HashSet<uint>();

		public static readonly List<IDestructible> DetectedDestructibles = new List<IDestructible>();
	}
}
