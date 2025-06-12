using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class MovementInaccuracyModule : ModuleBase, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule
{
	public readonly struct MovementInaccuracyDefinition
	{
		public readonly Firearm Template;

		private readonly float _minInaccuracy;

		private readonly float _maxInaccuracy;

		private readonly float _lightest;

		private readonly float _shortest;

		private readonly float _heaviest;

		private readonly float _longest;

		public MovementInaccuracyDefinition(Firearm template, float minInaccuracy, float maxInaccuracy)
		{
			this.Template = template;
			this._minInaccuracy = minInaccuracy;
			this._maxInaccuracy = maxInaccuracy;
			MovementInaccuracyDefinition.GetFirearmRamapValues(template, out this._lightest, out this._shortest, out this._heaviest, out this._longest);
		}

		public float Evaluate(Firearm firearm)
		{
			float num = firearm.AttachmentsValue(AttachmentParam.RunningInaccuracyMultiplier);
			float num2 = Mathf.InverseLerp(this._shortest, this._longest, firearm.Length);
			float num3 = Mathf.InverseLerp(this._lightest, this._heaviest, firearm.Weight);
			float t = (num2 + num3) / 2f;
			return Mathf.Lerp(this._minInaccuracy, this._maxInaccuracy, t) * num;
		}

		private static void GetFirearmRamapValues(Firearm firearm, out float lightest, out float shortest, out float heaviest, out float longest)
		{
			lightest = firearm.BaseWeight;
			shortest = firearm.BaseLength;
			heaviest = firearm.BaseWeight;
			longest = firearm.BaseLength;
			AttachmentSlot[] values = EnumUtils<AttachmentSlot>.Values;
			foreach (AttachmentSlot slot in values)
			{
				if (MovementInaccuracyDefinition.TryGetSlotRamapValues(firearm, slot, out var lightest2, out var shortest2, out var heaviest2, out var longest2))
				{
					lightest += lightest2;
					shortest += shortest2;
					heaviest += heaviest2;
					longest += longest2;
				}
			}
		}

		private static bool TryGetSlotRamapValues(Firearm firearm, AttachmentSlot slot, out float lightest, out float shortest, out float heaviest, out float longest)
		{
			lightest = float.MaxValue;
			shortest = float.MaxValue;
			heaviest = float.MinValue;
			longest = float.MinValue;
			bool result = false;
			Attachment[] attachments = firearm.Attachments;
			foreach (Attachment attachment in attachments)
			{
				if (attachment.Slot == slot)
				{
					result = true;
					lightest = Mathf.Min(lightest, attachment.Weight);
					shortest = Mathf.Min(shortest, attachment.Length);
					heaviest = Mathf.Max(heaviest, attachment.Weight);
					longest = Mathf.Max(longest, attachment.Length);
				}
			}
			return result;
		}
	}

	private static readonly Dictionary<ItemType, MovementInaccuracyDefinition> CachedDefinitions = new Dictionary<ItemType, MovementInaccuracyDefinition>();

	private const float JumpingPenalty = 3f;

	[field: SerializeField]
	public float MinPenalty { get; private set; }

	[field: SerializeField]
	public float MaxPenalty { get; private set; }

	public DisplayInaccuracyValues DisplayInaccuracy => new DisplayInaccuracyValues(0f, 0f, this.TargetRunningInaccuracy);

	public float Inaccuracy
	{
		get
		{
			float targetRunningInaccuracy = this.TargetRunningInaccuracy;
			if (!(base.Firearm.Owner.roleManager.CurrentRole is IFpcRole fpcRole))
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
			if (MovementInaccuracyModule.CachedDefinitions.TryGetValue(itemTypeId, out var value))
			{
				return value.Evaluate(base.Firearm);
			}
			MovementInaccuracyDefinition value2 = new MovementInaccuracyDefinition(base.Firearm, this.MinPenalty, this.MaxPenalty);
			MovementInaccuracyModule.CachedDefinitions.Add(itemTypeId, value2);
			return value2.Evaluate(base.Firearm);
		}
	}

	[ContextMenu("Clear Cache")]
	private void ClearCache()
	{
		MovementInaccuracyModule.CachedDefinitions.Clear();
	}
}
