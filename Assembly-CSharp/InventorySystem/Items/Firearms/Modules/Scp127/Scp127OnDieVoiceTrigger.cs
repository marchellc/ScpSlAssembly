using AudioPooling;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127OnDieVoiceTrigger : Scp127VoiceTriggerBase
{
	[SerializeField]
	private Scp127VoiceLineCollection _ownerDiedLines;

	protected override MixerChannel DefaultAudioMixerChannel => MixerChannel.NoDucking;

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		Scp127VoiceLineManagerModule.OnBeforeFriendshipReset += OnAnyPlayerDied;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		Scp127VoiceLineManagerModule.OnBeforeFriendshipReset -= OnAnyPlayerDied;
	}

	private void OnAnyPlayerDied(ReferenceHub deadHub)
	{
		if (base.IsServer && !(deadHub != base.Item.Owner))
		{
			base.ServerPlayVoiceLineFromCollection(this._ownerDiedLines, null, VoiceLinePriority.VeryHigh);
		}
	}
}
