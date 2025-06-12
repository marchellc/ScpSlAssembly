using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio;

public class RadioThirdperson : IdleThirdpersonItem
{
	[SerializeField]
	private SpatializedRadioPlaybackBase _playback;

	private uint _netId;

	private sbyte _prevVal;

	protected override void Update()
	{
		base.Update();
		if (!RadioMessages.SyncedRangeLevels.TryGetValue(this._netId, out var value))
		{
			return;
		}
		sbyte range = (sbyte)value.Range;
		if (range != this._prevVal)
		{
			if (range < 0)
			{
				this._playback.gameObject.SetActive(value: false);
			}
			else
			{
				this._playback.gameObject.SetActive(value: true);
				this._playback.RangeId = range;
			}
			this._prevVal = range;
		}
	}

	internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		base.Initialize(subcontroller, id);
		this._prevVal = sbyte.MinValue;
		this._netId = subcontroller.Model.OwnerHub.netId;
		this._playback.IgnoredNetId = this._netId;
	}
}
