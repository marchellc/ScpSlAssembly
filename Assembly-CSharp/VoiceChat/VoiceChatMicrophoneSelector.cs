using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VoiceChat
{
	public class VoiceChatMicrophoneSelector : TMP_Dropdown
	{
		protected override void Awake()
		{
			base.Awake();
			this.RefreshOptions();
			string text;
			if (VoiceChatMicrophoneSelector.TryGetPreferredMicrophone(out text))
			{
				base.value = 0;
				for (int i = 0; i < base.options.Count; i++)
				{
					if (!(base.options[i].text != text))
					{
						base.value = i;
						break;
					}
				}
			}
			base.onValueChanged.AddListener(new UnityAction<int>(this.OnValueChanged));
		}

		private void RefreshOptions()
		{
			string[] devices = Microphone.devices;
			int num = devices.Length;
			if (num == 0)
			{
				base.options = VoiceChatMicrophoneSelector._noMicError;
				return;
			}
			this.SetOption(0, VoiceChatMicrophoneSelector._defaultOption);
			for (int i = 0; i < num; i++)
			{
				this.SetOption(i + 1, devices[i]);
			}
			num++;
			if (num == base.options.Count)
			{
				return;
			}
			base.options.RemoveRange(num, base.options.Count - num);
		}

		private void SetOption(int index, string text)
		{
			while (index >= base.options.Count)
			{
				base.options.Add(new TMP_Dropdown.OptionData());
			}
			base.options[index].text = text;
		}

		private void Update()
		{
			this.RefreshOptions();
		}

		private void OnValueChanged(int i)
		{
			string text = ((i == 0) ? string.Empty : base.options[i].text);
			PlayerPrefsSl.Set("VcMicName", text);
			VoiceChatMicCapture.RestartRecording();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			TranslationReader.OnTranslationsRefreshed += delegate
			{
				VoiceChatMicrophoneSelector._defaultOption = TranslationReader.Get("Facility", 45, "Automatic (System Default)");
				VoiceChatMicrophoneSelector._noMicError = new List<TMP_Dropdown.OptionData>
				{
					new TMP_Dropdown.OptionData(TranslationReader.Get("Facility", 46, "No Microphone Detected!"))
				};
			};
		}

		public static bool TryGetPreferredMicrophone(out string mic)
		{
			string[] devices = Microphone.devices;
			if (devices.Length == 0)
			{
				mic = null;
				return false;
			}
			mic = PlayerPrefsSl.Get("VcMicName", string.Empty);
			if (!string.IsNullOrEmpty(mic) && !devices.Contains(mic))
			{
				mic = string.Empty;
			}
			return true;
		}

		private const string PrefsKeyMicName = "VcMicName";

		private static string _defaultOption;

		private static List<TMP_Dropdown.OptionData> _noMicError;
	}
}
