using System;
using InventorySystem.Items.Firearms.Modules.Misc;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

public class Scp127AnimEventVoiceTrigger : Scp127VoiceTriggerBase
{
	private VoiceLinePriority? _priorityOverride;

	[ExposedFirearmEvent]
	public void Play(Scp127VoiceLineCollection collection)
	{
		if (base.IsServer)
		{
			if (FirearmEvent.CurrentlyInvokedEvent == null)
			{
				throw new InvalidOperationException("Scp127AnimEventVoiceTrigger can only be called through an event.");
			}
			ServerPlayVoiceLineFromCollection(collection, null, _priorityOverride ?? VoiceLinePriority.Normal);
			_priorityOverride = null;
		}
	}

	[ExposedFirearmEvent]
	public void SetPriority(int priority)
	{
		_priorityOverride = (VoiceLinePriority)priority;
	}
}
