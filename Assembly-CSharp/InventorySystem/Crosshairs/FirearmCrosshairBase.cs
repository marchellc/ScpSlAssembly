using System;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Crosshairs
{
	public abstract class FirearmCrosshairBase : MonoBehaviour
	{
		protected virtual float GetAlpha(Firearm firearm)
		{
			bool flag = firearm.HasDownsideFlag(AttachmentDescriptiveDownsides.Laser);
			bool flag2 = Cursor.visible || flag;
			if (flag || flag2)
			{
				return 0f;
			}
			float adsAmount = this.GetAdsAmount(firearm);
			return 1f - adsAmount;
		}

		private float GetAdsAmount(Firearm firearm)
		{
			IAdsModule adsModule;
			if (!firearm.TryGetModule(out adsModule, true))
			{
				return 0f;
			}
			return adsModule.AdsAmount * 3.2f;
		}

		private void Update()
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			Firearm firearm = referenceHub.inventory.CurInstance as Firearm;
			if (firearm == null || firearm == null)
			{
				return;
			}
			this._fadeGroup.alpha = this.GetAlpha(firearm);
			float num = 0f;
			ModuleBase[] modules = firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IInaccuracyProviderModule inaccuracyProviderModule = modules[i] as IInaccuracyProviderModule;
				if (inaccuracyProviderModule != null)
				{
					num += inaccuracyProviderModule.Inaccuracy;
				}
			}
			this.UpdateCrosshair(firearm, num);
		}

		protected abstract void UpdateCrosshair(Firearm firearm, float currentInaccuracy);

		[SerializeField]
		private CanvasGroup _fadeGroup;
	}
}
