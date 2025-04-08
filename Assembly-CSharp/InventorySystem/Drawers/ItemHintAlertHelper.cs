using System;
using System.Text;
using InventorySystem.GUI;
using UnityEngine;

namespace InventorySystem.Drawers
{
	public class ItemHintAlertHelper
	{
		public AlertContent Alert
		{
			get
			{
				if (this._equipElapsed < this._fadeInDelay)
				{
					return default(AlertContent);
				}
				if (this._equipElapsed > this._fadeOutDelay + this._fadeTransitionDur)
				{
					return default(AlertContent);
				}
				float num = Mathf.InverseLerp(this._fadeInDelay, this._fadeInDelay + this._fadeTransitionDur, this._equipElapsed);
				float num2 = Mathf.InverseLerp(this._fadeOutDelay + this._fadeTransitionDur, this._fadeOutDelay, this._equipElapsed);
				float num3 = num * num2;
				return new AlertContent(this._content, num3, AlertContent.ColorMode.Accented);
			}
		}

		public void Update()
		{
			if (this._aborted)
			{
				return;
			}
			this._equipElapsed += Time.deltaTime;
			if (this._equipElapsed < this._fadeOutDelay + this._fadeTransitionDur)
			{
				return;
			}
			this.Hide();
		}

		public void Hide()
		{
			this._aborted = true;
			this._equipElapsed = this._fadeOutDelay + this._fadeTransitionDur + 1f;
		}

		public void Reset()
		{
			this._aborted = false;
			this._equipElapsed = 0f;
		}

		public ItemHintAlertHelper(InventoryGuiTranslation translation, ActionName? key, float fadeTransitionDuration = 0.3f, float fadeInDelay = 1f, float fadeOutDelay = 6f)
		{
			this._content = ItemHintAlertHelper.ProcessTranslation(translation, key);
			this._fadeTransitionDur = fadeTransitionDuration;
			this._fadeInDelay = fadeInDelay;
			this._fadeOutDelay = fadeOutDelay;
		}

		public ItemHintAlertHelper(InventoryGuiTranslation translation0, ActionName? key0, InventoryGuiTranslation translation1, ActionName? key1, float fadeTransitionDuration = 0.3f, float fadeInDelay = 1f, float fadeOutDelay = 8f)
		{
			ItemHintAlertHelper.SbNonAlloc.Clear();
			ItemHintAlertHelper.SbNonAlloc.AppendLine(ItemHintAlertHelper.ProcessTranslation(translation0, key0));
			ItemHintAlertHelper.SbNonAlloc.AppendLine(ItemHintAlertHelper.ProcessTranslation(translation1, key1));
			this._content = ItemHintAlertHelper.SbNonAlloc.ToString();
			this._fadeTransitionDur = fadeTransitionDuration;
			this._fadeInDelay = fadeInDelay;
			this._fadeOutDelay = fadeOutDelay;
		}

		public ItemHintAlertHelper(InventoryGuiTranslation translation0, ActionName? key0, InventoryGuiTranslation translation1, ActionName? key1, InventoryGuiTranslation translation2, ActionName? key2, float fadeTransitionDuration = 0.3f, float fadeInDelay = 1f, float fadeOutDelay = 10f)
		{
			ItemHintAlertHelper.SbNonAlloc.Clear();
			ItemHintAlertHelper.SbNonAlloc.AppendLine(ItemHintAlertHelper.ProcessTranslation(translation0, key0));
			ItemHintAlertHelper.SbNonAlloc.AppendLine(ItemHintAlertHelper.ProcessTranslation(translation1, key1));
			ItemHintAlertHelper.SbNonAlloc.AppendLine(ItemHintAlertHelper.ProcessTranslation(translation2, key2));
			this._content = ItemHintAlertHelper.SbNonAlloc.ToString();
			this._fadeTransitionDur = fadeTransitionDuration;
			this._fadeInDelay = fadeInDelay;
			this._fadeOutDelay = fadeOutDelay;
		}

		private static string ProcessTranslation(InventoryGuiTranslation translation, ActionName? key)
		{
			if (!TranslationReader.EverLoaded)
			{
				return string.Empty;
			}
			if (key == null)
			{
				return Translations.Get<InventoryGuiTranslation>(translation);
			}
			ReadableKeyCode readableKeyCode = new ReadableKeyCode(key.Value);
			return string.Format(Translations.Get<InventoryGuiTranslation>(translation), string.Format("${0}$", readableKeyCode));
		}

		private static readonly StringBuilder SbNonAlloc = new StringBuilder();

		private readonly string _content;

		private readonly float _fadeTransitionDur;

		private readonly float _fadeInDelay;

		private readonly float _fadeOutDelay;

		private const float DefaultFadeTransition = 0.3f;

		private const float DefaultFadeInDelay = 1f;

		private float _equipElapsed;

		private bool _aborted;
	}
}
