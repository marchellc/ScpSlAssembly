using AudioPooling;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127OnKillVoiceTrigger : Scp127VoiceTriggerBase
{
	[SerializeField]
	private Scp127VoiceLineCollection _onKillLines;

	protected override MixerChannel DefaultAudioMixerChannel => MixerChannel.NoDucking;

	protected override void RegisterEvents()
	{
		base.RegisterEvents();
		Scp127TierManagerModule.ServerOnKilled += OnKilled;
	}

	protected override void UnregisterEvents()
	{
		base.UnregisterEvents();
		Scp127TierManagerModule.ServerOnKilled -= OnKilled;
	}

	private void OnKilled(Firearm scp127, ReferenceHub deadHub)
	{
		if (!(scp127 != base.Firearm) && _onKillLines.TryGetRandom(deadHub.GetRoleId(), out var voiceLine))
		{
			ServerPlayVoiceLine(voiceLine);
		}
	}
}
