using System;
using InventorySystem.Items.Autosync;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	public static class AttachmentPreview
	{
		public static Firearm Get(ItemType id, uint attachmentsCode, bool reValidate = false)
		{
			Firearm firearm;
			if (!AttachmentPreview.TryGet(id, attachmentsCode, reValidate, out firearm))
			{
				throw new InvalidOperationException(string.Format("Unable to create a preview for {0} - it's not a valid firearm id!", id));
			}
			return firearm;
		}

		public static Firearm Get(ItemIdentifier id, bool reValidate = false)
		{
			Firearm firearm;
			if (!AttachmentPreview.TryGet(id, reValidate, out firearm))
			{
				throw new InvalidOperationException(string.Format("Unable to create a preview for {0} - it's not a valid firearm id!", id));
			}
			return firearm;
		}

		public static bool TryGet(ItemType id, uint attachmentsCode, bool reValidate, out Firearm result)
		{
			if (!AttachmentPreview.TryGetOrAddInstance(id, out result))
			{
				return false;
			}
			result.ApplyAttachmentsCode(attachmentsCode, reValidate);
			return true;
		}

		public static bool TryGet(ItemIdentifier id, bool reValidate, out Firearm result)
		{
			uint num;
			if (!AttachmentCodeSync.TryGet(id.SerialNumber, out num))
			{
				num = 0U;
				reValidate = true;
			}
			return AttachmentPreview.TryGet(id.TypeId, num, reValidate, out result);
		}

		private static bool TryGetOrAddInstance(ItemType id, out Firearm instance)
		{
			if (AttachmentPreview.HasInstances[(int)id])
			{
				instance = AttachmentPreview.Instances[(int)id];
				return true;
			}
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(id, out firearm))
			{
				instance = null;
				return false;
			}
			instance = global::UnityEngine.Object.Instantiate<Firearm>(firearm);
			instance.InstantiationStatus = AutosyncInstantiationStatus.SimulatedInstance;
			instance.InitializeSubcomponents();
			global::UnityEngine.Object.DontDestroyOnLoad(instance);
			AttachmentPreview.Instances[(int)id] = instance;
			AttachmentPreview.HasInstances[(int)id] = true;
			return true;
		}

		private static readonly Firearm[] Instances = new Firearm[EnumUtils<ItemType>.Values.Length];

		private static readonly bool[] HasInstances = new bool[AttachmentPreview.Instances.Length];
	}
}
