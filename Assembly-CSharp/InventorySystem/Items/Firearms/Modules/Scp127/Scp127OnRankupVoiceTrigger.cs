using AudioPooling;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127OnRankupVoiceTrigger : Scp127VoiceTriggerBase
{
	[SerializeField]
	private Scp127VoiceLineCollection _rankupLines;

	protected override MixerChannel DefaultAudioMixerChannel => MixerChannel.NoDucking;

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		Scp127TierManagerModule.ServerOnLevelledUp += OnLevelledUp;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		Scp127TierManagerModule.ServerOnLevelledUp -= OnLevelledUp;
	}

	private void OnLevelledUp(Firearm fa)
	{
		if (!(fa != base.Firearm))
		{
			base.ServerPlayVoiceLineFromCollection(this._rankupLines, null, VoiceLinePriority.High);
		}
	}
}
