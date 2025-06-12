using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079NotificationManager : Scp079GuiElementBase
{
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

	private void Awake()
	{
		Scp079NotificationManager._singleton = this;
		Scp079NotificationManager._singletonSet = true;
	}

	private void OnDestroy()
	{
		if (!(Scp079NotificationManager._singleton != this))
		{
			Scp079NotificationManager._singletonSet = false;
		}
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
				base.PlaySound(this._sounds[(int)sound]);
			}
			if (content.Delete)
			{
				scp079NotificationEntry.gameObject.SetActive(value: false);
				this._textPool.Enqueue(scp079NotificationEntry);
				this._spawnedTexts.RemoveAt(i);
				i--;
			}
		}
	}

	private void SpawnNotification(IScp079Notification notification)
	{
		if (!this._textPool.TryDequeue(out var result))
		{
			result = Object.Instantiate(this._template, this._template.transform.parent);
		}
		this._spawnedTexts.Add(result);
		result.Content = notification;
		result.Text.text = string.Empty;
		result.Text.rectTransform.sizeDelta = this._defaultSize;
		result.gameObject.SetActive(value: true);
		result.transform.SetAsLastSibling();
	}

	public static void AddNotification(IScp079Notification handler)
	{
		if (Scp079NotificationManager._singletonSet)
		{
			Scp079NotificationManager._singleton.SpawnNotification(handler);
		}
	}

	public static void AddNotification(string notification)
	{
		Scp079NotificationManager.AddNotification(new Scp079SimpleNotification(notification));
	}

	public static void AddNotification(Scp079HudTranslation translation)
	{
		Scp079NotificationManager.AddNotification(Translations.Get(translation));
	}

	public static void AddNotification(Scp079HudTranslation translation, params object[] format)
	{
		Scp079NotificationManager.AddNotification(string.Format(Translations.Get(translation), format));
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
}
