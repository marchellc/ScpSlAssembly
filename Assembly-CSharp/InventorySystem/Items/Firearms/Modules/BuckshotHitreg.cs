using System;
using System.Collections.Generic;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class BuckshotHitreg : HitscanHitregModuleBase
	{
		public BuckshotSettings BasePattern { get; private set; }

		public override Type CrosshairType
		{
			get
			{
				return typeof(BuckshotCrosshair);
			}
		}

		public float BuckshotScale
		{
			get
			{
				return this.ActivePattern.OverallScale * base.Firearm.AttachmentsValue(AttachmentParam.SpreadMultiplier);
			}
		}

		public override bool UseHitboxMultipliers
		{
			get
			{
				return false;
			}
		}

		private BuckshotSettings ActivePattern
		{
			get
			{
				foreach (Attachment attachment in base.Firearm.Attachments)
				{
					if (attachment.IsEnabled)
					{
						BuckshotPatternAttachment buckshotPatternAttachment = attachment as BuckshotPatternAttachment;
						if (buckshotPatternAttachment != null)
						{
							return buckshotPatternAttachment.Pattern;
						}
					}
				}
				return this.BasePattern;
			}
		}

		protected override void Fire()
		{
			this._hitCounter.Clear();
			Ray ray = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
			BuckshotSettings activePattern = this.ActivePattern;
			float num = base.Firearm.AttachmentsValue(AttachmentParam.SpreadPredictability);
			float num2 = 1f - Mathf.Clamp01(1f - activePattern.Randomness) * num;
			float buckshotScale = this.BuckshotScale;
			float num3 = 0f;
			foreach (Vector2 vector in activePattern.PredefinedPellets)
			{
				Vector3 pelletDirection = this.GetPelletDirection(vector, buckshotScale, num2, ray.direction);
				float num4;
				base.ServerPerformHitscan(new Ray(ray.origin, pelletDirection), out num4);
				num3 += num4;
			}
			foreach (KeyValuePair<IDestructible, int> keyValuePair in this._hitCounter)
			{
				this.ServerLastDamagedTargets.Add(keyValuePair.Key);
			}
			this.SendHitmarker(num3);
		}

		protected override float DamageAtDistance(float dist)
		{
			return base.DamageAtDistance(dist) / (float)this.ActivePattern.MaxHits;
		}

		protected override float ServerProcessTargetHit(IDestructible dest, RaycastHit hitInfo)
		{
			int valueOrDefault = this._hitCounter.GetValueOrDefault(dest);
			if (valueOrDefault >= this.ActivePattern.MaxHits)
			{
				return 0f;
			}
			this._hitCounter[dest] = valueOrDefault + 1;
			return base.ServerProcessTargetHit(dest, hitInfo);
		}

		private Vector3 GetPelletDirection(Vector2 pelletVector, float scale, float randomness, Vector3 fwdDirection)
		{
			Vector2 insideUnitCircle = global::UnityEngine.Random.insideUnitCircle;
			Vector2 vector = Vector2.Lerp(pelletVector, insideUnitCircle, randomness) * scale;
			Transform playerCameraReference = base.Firearm.Owner.PlayerCameraReference;
			fwdDirection = Quaternion.AngleAxis(vector.x, playerCameraReference.up) * fwdDirection;
			fwdDirection = Quaternion.AngleAxis(vector.y, playerCameraReference.right) * fwdDirection;
			return fwdDirection;
		}

		private readonly Dictionary<IDestructible, int> _hitCounter = new Dictionary<IDestructible, int>();
	}
}
