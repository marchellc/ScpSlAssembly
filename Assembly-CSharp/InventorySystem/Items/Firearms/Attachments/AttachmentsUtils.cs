using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments.Components;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	public static class AttachmentsUtils
	{
		public static event Action<Firearm> OnAttachmentsApplied;

		public static int TotalNumberOfParams
		{
			get
			{
				if (AttachmentsUtils._paramNumberCache <= 0)
				{
					AttachmentsUtils._paramNumberCache = EnumUtils<AttachmentParam>.Values.Length;
				}
				return AttachmentsUtils._paramNumberCache;
			}
		}

		public static uint GetCurrentAttachmentsCode(this Firearm firearm)
		{
			uint num = 1U;
			uint num2 = 0U;
			for (int i = 0; i < firearm.Attachments.Length; i++)
			{
				if (firearm.Attachments[i].IsEnabled)
				{
					num2 += num;
				}
				num *= 2U;
			}
			return num2;
		}

		public static uint GetRandomAttachmentsCode(ItemType firearmType)
		{
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(firearmType, out firearm))
			{
				return 0U;
			}
			int num = firearm.Attachments.Length;
			bool[] array = new bool[num];
			int i = 0;
			while (i < num)
			{
				AttachmentSlot slot = firearm.Attachments[i].Slot;
				for (int j = i; j < num; j++)
				{
					if (j + 1 >= num || firearm.Attachments[j + 1].Slot != slot)
					{
						array[global::UnityEngine.Random.Range(i, j + 1)] = true;
						i = j + 1;
						break;
					}
				}
			}
			uint num2 = 1U;
			uint num3 = 0U;
			for (int k = 0; k < num; k++)
			{
				if (array[k])
				{
					num3 += num2;
				}
				num2 *= 2U;
			}
			return num3;
		}

		public static bool TryGetAttachmentWithId(this Firearm firearm, int id, out Attachment att)
		{
			SubcomponentBase subcomponentBase;
			if (firearm.TryGetSubcomponentFromId(id, out subcomponentBase))
			{
				Attachment attachment = subcomponentBase as Attachment;
				if (attachment != null)
				{
					att = attachment;
					return true;
				}
			}
			att = null;
			return false;
		}

		public static float AttachmentsValue(this Firearm firearm, AttachmentParam param)
		{
			AttachmentParameterDefinition definitionOfParam = AttachmentsUtils.GetDefinitionOfParam(param);
			float num = definitionOfParam.DefaultValue;
			int num2 = firearm.Attachments.Length;
			for (int i = 0; i < num2; i++)
			{
				Attachment attachment = firearm.Attachments[i];
				float num3;
				if (attachment.IsEnabled && attachment.TryGetActiveValue(param, out num3))
				{
					num = AttachmentsUtils.MixValue(num, num3, definitionOfParam.MixingMode);
				}
			}
			if (!firearm.HasOwner)
			{
				return num;
			}
			for (int j = 0; j < firearm.Owner.playerEffectsController.EffectsLength; j++)
			{
				IWeaponModifierPlayerEffect weaponModifierPlayerEffect = firearm.Owner.playerEffectsController.AllEffects[j] as IWeaponModifierPlayerEffect;
				float num4;
				if (weaponModifierPlayerEffect != null && weaponModifierPlayerEffect.ParamsActive && weaponModifierPlayerEffect.TryGetWeaponParam(param, out num4))
				{
					num = AttachmentsUtils.MixValue(num, num4, definitionOfParam.MixingMode);
				}
			}
			return AttachmentsUtils.ClampValue(num, definitionOfParam);
		}

		public static float ProcessValue(this Firearm firearm, float value, AttachmentParam param)
		{
			float num = firearm.AttachmentsValue(param);
			switch (AttachmentsUtils.GetDefinitionOfParam(param).MixingMode)
			{
			case ParameterMixingMode.Override:
				return num;
			case ParameterMixingMode.Additive:
				return value + num;
			case ParameterMixingMode.Percent:
				return value * num;
			default:
				return value;
			}
		}

		public static bool HasAdvantageFlag(this Firearm firearm, AttachmentDescriptiveAdvantages flag)
		{
			int num = firearm.Attachments.Length;
			for (int i = 0; i < num; i++)
			{
				Attachment attachment = firearm.Attachments[i];
				if (attachment.IsEnabled && attachment.DescriptivePros.HasFlagFast(flag))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasDownsideFlag(this Firearm firearm, AttachmentDescriptiveDownsides flag)
		{
			int num = firearm.Attachments.Length;
			for (int i = 0; i < num; i++)
			{
				Attachment attachment = firearm.Attachments[i];
				if (attachment.IsEnabled && attachment.DescriptiveCons.HasFlagFast(flag))
				{
					return true;
				}
			}
			return false;
		}

		public static float MixValue(float originalValue, float modifierValue, ParameterMixingMode mixMode)
		{
			switch (mixMode)
			{
			case ParameterMixingMode.Override:
				originalValue = modifierValue;
				break;
			case ParameterMixingMode.Additive:
				originalValue += modifierValue;
				break;
			case ParameterMixingMode.Percent:
				originalValue += modifierValue - 1f;
				break;
			}
			return originalValue;
		}

		private static float ClampValue(float f, AttachmentParameterDefinition definition)
		{
			return Mathf.Clamp(f, definition.MinValue, definition.MaxValue);
		}

		private static AttachmentParameterDefinition GetDefinitionOfParam(AttachmentParam param)
		{
			if (!AttachmentsUtils._mixingModesCacheSet)
			{
				AttachmentsUtils._cachedDefitionons = new AttachmentParameterDefinition[AttachmentsUtils.TotalNumberOfParams];
				AttachmentsUtils._readyMixingModes = new bool[AttachmentsUtils.TotalNumberOfParams];
				AttachmentsUtils._mixingModesCacheSet = true;
			}
			if (AttachmentsUtils._readyMixingModes[(int)param])
			{
				return AttachmentsUtils._cachedDefitionons[(int)param];
			}
			AttachmentParameterDefinition attachmentParameterDefinition;
			if (!AttachmentParameterDefinition.Definitions.TryGetValue(param, out attachmentParameterDefinition))
			{
				throw new Exception(string.Format("Parameter {0} is not defined!", param));
			}
			AttachmentsUtils._readyMixingModes[(int)param] = true;
			AttachmentsUtils._cachedDefitionons[(int)param] = attachmentParameterDefinition;
			return attachmentParameterDefinition;
		}

		public static void ApplyAttachmentsCode(this Firearm firearm, uint code, bool reValidate)
		{
			if (reValidate)
			{
				code = firearm.ValidateAttachmentsCode(code);
			}
			uint num = 1U;
			for (int i = 0; i < firearm.Attachments.Length; i++)
			{
				firearm.Attachments[i].IsEnabled = (code & num) == num;
				num *= 2U;
			}
			SubcomponentBase[] allSubcomponents = firearm.AllSubcomponents;
			for (int j = 0; j < allSubcomponents.Length; j++)
			{
				FirearmSubcomponentBase firearmSubcomponentBase = allSubcomponents[j] as FirearmSubcomponentBase;
				if (firearmSubcomponentBase != null)
				{
					firearmSubcomponentBase.OnAttachmentsApplied();
				}
			}
			Action<Firearm> onAttachmentsApplied = AttachmentsUtils.OnAttachmentsApplied;
			if (onAttachmentsApplied == null)
			{
				return;
			}
			onAttachmentsApplied(firearm);
		}

		public static uint ValidateAttachmentsCode(this Firearm firearm, uint code)
		{
			uint num = 0U;
			uint num2 = 1U;
			HashSet<AttachmentSlot> hashSet = HashSetPool<AttachmentSlot>.Shared.Rent();
			foreach (Attachment attachment in firearm.Attachments)
			{
				hashSet.Add(attachment.Slot);
			}
			for (int j = 0; j < firearm.Attachments.Length; j++)
			{
				if ((code & num2) == num2 && hashSet.Remove(firearm.Attachments[j].Slot))
				{
					num += num2;
				}
				num2 *= 2U;
			}
			foreach (AttachmentSlot attachmentSlot in hashSet)
			{
				for (int k = 0; k < firearm.Attachments.Length; k++)
				{
					if (attachmentSlot == firearm.Attachments[k].Slot)
					{
						uint num3 = 1U;
						for (int l = 0; l < k; l++)
						{
							num3 *= 2U;
						}
						num += num3;
						break;
					}
				}
			}
			HashSetPool<AttachmentSlot>.Shared.Return(hashSet);
			return num;
		}

		public static void GetDefaultLengthAndWeight(this Firearm fa, out float length, out float weight)
		{
			HashSet<AttachmentSlot> hashSet = HashSetPool<AttachmentSlot>.Shared.Rent();
			length = fa.BaseLength;
			weight = fa.BaseWeight;
			for (int i = 0; i < fa.Attachments.Length; i++)
			{
				if (hashSet.Add(fa.Attachments[i].Slot))
				{
					length += fa.Attachments[i].Length;
					weight += fa.Attachments[i].Weight;
				}
			}
			HashSetPool<AttachmentSlot>.Shared.Return(hashSet);
		}

		public static bool HasFlagFast(this AttachmentDescriptiveAdvantages flags, AttachmentDescriptiveAdvantages flag)
		{
			return (flags & flag) == flag;
		}

		public static bool HasFlagFast(this AttachmentDescriptiveDownsides flags, AttachmentDescriptiveDownsides flag)
		{
			return (flags & flag) == flag;
		}

		private static int _paramNumberCache;

		private static AttachmentParameterDefinition[] _cachedDefitionons;

		private static bool[] _readyMixingModes;

		private static bool _mixingModesCacheSet;
	}
}
