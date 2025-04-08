using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules.Misc;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AnimatorStateSetterModule : ModuleBase
	{
		[ExposedFirearmEvent]
		public void SkipFrames(int frames)
		{
			FirearmEvent currentlyInvokedEvent = FirearmEvent.CurrentlyInvokedEvent;
			if (currentlyInvokedEvent == null)
			{
				return;
			}
			float totalSpeedMultiplier = currentlyInvokedEvent.LastInvocation.TotalSpeedMultiplier;
			if (totalSpeedMultiplier <= 0f)
			{
				return;
			}
			float num = 1f / (currentlyInvokedEvent.Clip.frameRate * totalSpeedMultiplier);
			base.Firearm.AnimForceUpdate(num * (float)frames);
		}

		[ExposedFirearmEvent]
		public void SetAttachmentReady(Attachment attachment)
		{
			this._activeFlags.Add(attachment);
		}

		public bool GetReadyFlag(Attachment attachment)
		{
			return this._activeFlags.Contains(attachment);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this.ExposeParameters();
			this.ExposeAttachments();
			if (!this._setRoleId)
			{
				return;
			}
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.RoleId, (int)base.Firearm.Owner.GetRoleId(), false);
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._activeFlags.Clear();
		}

		private void ExposeParameters()
		{
			foreach (AttachmentParam attachmentParam in this._exposedParams)
			{
				int num = AnimatorStateSetterModule.EnumToHash<AttachmentParam>(attachmentParam, AnimatorStateSetterModule.ParamHash, new Func<string, int>(Animator.StringToHash));
				base.Firearm.AnimSetFloat(num, base.Firearm.AttachmentsValue(attachmentParam), false);
			}
		}

		private void ExposeAttachments()
		{
			Attachment[] attachments = base.Firearm.Attachments;
			foreach (AttachmentSlot attachmentSlot in this._exposedSlots)
			{
				int num = -1;
				for (int j = 0; j < attachments.Length; j++)
				{
					if (attachments[j].Slot == attachmentSlot)
					{
						num++;
						if (attachments[j].IsEnabled)
						{
							break;
						}
					}
				}
				if (num >= 0)
				{
					int num2 = AnimatorStateSetterModule.EnumToHash<AttachmentSlot>(attachmentSlot, AnimatorStateSetterModule.SlotHash, new Func<string, int>(AnimatorStateSetterModule.GenerateAttachmentIdHash));
					base.Firearm.AnimSetInt(num2, num, false);
				}
			}
		}

		private static int GenerateAttachmentIdHash(string enumName)
		{
			return Animator.StringToHash(enumName + "Id");
		}

		private static int EnumToHash<T>(T enumValue, Dictionary<T, int> cache, Func<string, int> hashFunc) where T : struct, Enum, IConvertible
		{
			int num;
			if (!cache.TryGetValue(enumValue, out num))
			{
				int num2 = enumValue.ToInt32(null);
				num = hashFunc(EnumUtils<T>.Names[num2]);
				cache[enumValue] = num;
			}
			return num;
		}

		private static readonly Dictionary<AttachmentSlot, int> SlotHash = new Dictionary<AttachmentSlot, int>();

		private static readonly Dictionary<AttachmentParam, int> ParamHash = new Dictionary<AttachmentParam, int>();

		[SerializeField]
		private AttachmentSlot[] _exposedSlots;

		[SerializeField]
		private AttachmentParam[] _exposedParams;

		[SerializeField]
		private bool _setRoleId;

		private readonly HashSet<Attachment> _activeFlags = new HashSet<Attachment>();
	}
}
