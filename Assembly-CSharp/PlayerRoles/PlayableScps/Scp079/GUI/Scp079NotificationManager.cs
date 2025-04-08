using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079NotificationManager : Scp079GuiElementBase
	{
		private void Awake()
		{
			Scp079NotificationManager._singleton = this;
			Scp079NotificationManager._singletonSet = true;
		}

		private void OnDestroy()
		{
			if (Scp079NotificationManager._singleton != this)
			{
				return;
			}
			Scp079NotificationManager._singletonSet = false;
		}

		private void Update()
		{
			for (int i = 0; i < this._spawnedTexts.Count; i++)
			{
				Scp079NotificationEntry scp079NotificationEntry = this._spawnedTexts[i];
				IScp079Notification content = scp079NotificationEntry.Content;
				scp079NotificationEntry.Text.text = content.DisplayedText;
				scp079NotificationEntry.Text.alpha = Mathf.Clamp01(content.Opacity);
				scp079NotificationEntry.Text.rectTransform.sizeDelta = new Vector2(this._defaultSize.x, content.Height);
				NotificationSound sound = content.Sound;
				if (sound != NotificationSound.None)
				{
					base.PlaySound(this._sounds[(int)sound], 1f);
				}
				if (content.Delete)
				{
					scp079NotificationEntry.gameObject.SetActive(false);
					this._textPool.Enqueue(scp079NotificationEntry);
					this._spawnedTexts.RemoveAt(i);
					i--;
				}
			}
		}

		private void SpawnNotification(IScp079Notification notification)
		{
			Scp079NotificationEntry scp079NotificationEntry;
			if (!this._textPool.TryDequeue(out scp079NotificationEntry))
			{
				scp079NotificationEntry = global::UnityEngine.Object.Instantiate<Scp079NotificationEntry>(this._template, this._template.transform.parent);
			}
			this._spawnedTexts.Add(scp079NotificationEntry);
			scp079NotificationEntry.Content = notification;
			scp079NotificationEntry.Text.text = string.Empty;
			scp079NotificationEntry.Text.rectTransform.sizeDelta = this._defaultSize;
			scp079NotificationEntry.gameObject.SetActive(true);
			scp079NotificationEntry.transform.SetAsLastSibling();
		}

		public static void AddNotification(IScp079Notification handler)
		{
			if (!Scp079NotificationManager._singletonSet)
			{
				return;
			}
			Scp079NotificationManager._singleton.SpawnNotification(handler);
		}

		public static void AddNotification(string notification)
		{
			Scp079NotificationManager.AddNotification(new Scp079SimpleNotification(notification, false));
		}

		public static void AddNotification(Scp079HudTranslation translation)
		{
			Scp079NotificationManager.AddNotification(Translations.Get<Scp079HudTranslation>(translation));
		}

		public static void AddNotification(Scp079HudTranslation translation, params object[] format)
		{
			Scp079NotificationManager.AddNotification(string.Format(Translations.Get<Scp079HudTranslation>(translation), format));
		}

		public static bool TryGetTextHeight(string content, out float height)
		{
			if (!Scp079NotificationManager._singletonSet)
			{
				height = 0f;
				return false;
			}
			TMP_Text text = Scp079NotificationManager._singleton._template.Text;
			text.text = content;
			height = text.preferredHeight;
			text.text = string.Empty;
			return true;
		}

		[SerializeField]
		private Scp079NotificationEntry _template;

		[SerializeField]
		private Vector2 _defaultSize;

		[SerializeField]
		private AudioClip[] _sounds;

		private readonly Queue<Scp079NotificationEntry> _textPool = new Queue<Scp079NotificationEntry>();

		private readonly List<Scp079NotificationEntry> _spawnedTexts = new List<Scp079NotificationEntry>();

		private static Scp079NotificationManager _singleton;

		private static bool _singletonSet;
	}
}
