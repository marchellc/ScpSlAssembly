using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class MovementInaccuracyModule : ModuleBase, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
	{
		public float MinPenalty { get; private set; }

		public float MaxPenalty { get; private set; }

		public DisplayInaccuracyValues DisplayInaccuracy
		{
			get
			{
				return new DisplayInaccuracyValues(0f, 0f, this.TargetRunningInaccuracy, 0f);
			}
		}

		public float Inaccuracy
		{
			get
			{
				float targetRunningInaccuracy = this.TargetRunningInaccuracy;
				IFpcRole fpcRole = base.Firearm.Owner.roleManager.CurrentRole as IFpcRole;
				if (fpcRole == null)
				{
					return 0f;
				}
				if (!fpcRole.FpcModule.IsGrounded)
				{
					return targetRunningInaccuracy + 3f;
				}
				float num = fpcRole.FpcModule.Motor.Velocity.MagnitudeIgnoreY();
				float sprintSpeed = fpcRole.FpcModule.SprintSpeed;
				float num2 = EaseInUtils.EaseInOutCubic(num / sprintSpeed);
				return targetRunningInaccuracy * num2;
			}
		}

		private float TargetRunningInaccuracy
		{
			get
			{
				ItemType itemTypeId = base.Firearm.ItemTypeId;
				MovementInaccuracyModule.MovementInaccuracyDefinition movementInaccuracyDefinition;
				if (MovementInaccuracyModule.CachedDefinitions.TryGetValue(itemTypeId, out movementInaccuracyDefinition))
				{
					return movementInaccuracyDefinition.Evaluate(base.Firearm);
				}
				MovementInaccuracyModule.MovementInaccuracyDefinition movementInaccuracyDefinition2 = new MovementInaccuracyModule.MovementInaccuracyDefinition(base.Firearm, this.MinPenalty, this.MaxPenalty);
				MovementInaccuracyModule.CachedDefinitions.Add(itemTypeId, movementInaccuracyDefinition2);
				return movementInaccuracyDefinition2.Evaluate(base.Firearm);
			}
		}

		[ContextMenu("Clear Cache")]
		private void ClearCache()
		{
			MovementInaccuracyModule.CachedDefinitions.Clear();
		}

		private static readonly Dictionary<ItemType, MovementInaccuracyModule.MovementInaccuracyDefinition> CachedDefinitions = new Dictionary<ItemType, MovementInaccuracyModule.MovementInaccuracyDefinition>();

		private const float JumpingPenalty = 3f;

		public readonly struct MovementInaccuracyDefinition
		{
			public MovementInaccuracyDefinition(Firearm template, float minInaccuracy, float maxInaccuracy)
			{
				this.Template = template;
				this._minInaccuracy = minInaccuracy;
				this._maxInaccuracy = maxInaccuracy;
				MovementInaccuracyModule.MovementInaccuracyDefinition.GetFirearmRamapValues(template, out this._lightest, out this._shortest, out this._heaviest, out this._longest);
			}

			public float Evaluate(Firearm firearm)
			{
				float num = firearm.AttachmentsValue(AttachmentParam.RunningInaccuracyMultiplier);
				float num2 = Mathf.InverseLerp(this._shortest, this._longest, firearm.Length);
				float num3 = Mathf.InverseLerp(this._lightest, this._heaviest, firearm.Weight);
				float num4 = (num2 + num3) / 2f;
				return Mathf.Lerp(this._minInaccuracy, this._maxInaccuracy, num4) * num;
			}

			private static void GetFirearmRamapValues(Firearm firearm, out float lightest, out float shortest, out float heaviest, out float longest)
			{
				lightest = firearm.BaseWeight;
				shortest = firearm.BaseLength;
				heaviest = firearm.BaseWeight;
				longest = firearm.BaseLength;
				foreach (AttachmentSlot attachmentSlot in EnumUtils<AttachmentSlot>.Values)
				{
					float num;
					float num2;
					float num3;
					float num4;
					if (MovementInaccuracyModule.MovementInaccuracyDefinition.TryGetSlotRamapValues(firearm, attachmentSlot, out num, out num2, out num3, out num4))
					{
						lightest += num;
						shortest += num2;
						heaviest += num3;
						longest += num4;
					}
				}
			}

			private static bool TryGetSlotRamapValues(Firearm firearm, AttachmentSlot slot, out float lightest, out float shortest, out float heaviest, out float longest)
			{
				lightest = float.MaxValue;
				shortest = float.MaxValue;
				heaviest = float.MinValue;
				longest = float.MinValue;
				bool flag = false;
				foreach (Attachment attachment in firearm.Attachments)
				{
					if (attachment.Slot == slot)
					{
						flag = true;
						lightest = Mathf.Min(lightest, attachment.Weight);
						shortest = Mathf.Min(shortest, attachment.Length);
						heaviest = Mathf.Max(heaviest, attachment.Weight);
						longest = Mathf.Max(longest, attachment.Length);
					}
				}
				return flag;
			}

			public readonly Firearm Template;

			private readonly float _minInaccuracy;

			private readonly float _maxInaccuracy;

			private readonly float _lightest;

			private readonly float _shortest;

			private readonly float _heaviest;

			private readonly float _longest;
		}
	}
}
