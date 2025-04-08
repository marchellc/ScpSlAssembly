using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class AudioManagerModule : MicroHidModuleBase
	{
		public static AudioController GetController(ushort serial)
		{
			AudioController audioController;
			if (!AudioManagerModule.Instances.TryGetValue(serial, out audioController))
			{
				audioController = global::UnityEngine.Object.Instantiate<AudioController>(AudioManagerModule._globalAudioControllerTemplate);
				audioController.Serial = serial;
				AudioManagerModule.Instances[serial] = audioController;
			}
			return audioController;
		}

		public static void RegisterDestroyed(AudioController destroyed)
		{
			AudioManagerModule.Instances.Remove(destroyed.Serial);
		}

		private static void UpdateInstance(CycleController cycleController)
		{
			MicroHidPhase phase = cycleController.Phase;
			if (phase != MicroHidPhase.Standby)
			{
				AudioManagerModule.GetController(cycleController.Serial).UpdateAudio(phase);
				return;
			}
			AudioController audioController;
			if (!AudioManagerModule.Instances.TryGetValue(cycleController.Serial, out audioController))
			{
				return;
			}
			audioController.UpdateAudio(phase);
		}

		internal override void TemplateUpdate()
		{
			base.TemplateUpdate();
			CycleSyncModule.ForEachController(new Action<CycleController>(AudioManagerModule.UpdateInstance));
		}

		internal override void OnTemplateReloaded(ModularAutosyncItem template, bool wasEverLoaded)
		{
			base.OnTemplateReloaded(template, wasEverLoaded);
			AudioManagerModule._globalAudioControllerTemplate = this._audioControllerPrefab;
		}

		private static readonly Dictionary<ushort, AudioController> Instances = new Dictionary<ushort, AudioController>();

		private static AudioController _globalAudioControllerTemplate;

		[SerializeField]
		private AudioController _audioControllerPrefab;
	}
}
