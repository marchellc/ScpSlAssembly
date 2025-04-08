using System;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio
{
	public class RadioThirdperson : IdleThirdpersonItem
	{
		protected override void Update()
		{
			base.Update();
			RadioStatusMessage radioStatusMessage;
			if (!RadioMessages.SyncedRangeLevels.TryGetValue(this._netId, out radioStatusMessage))
			{
				return;
			}
			sbyte range = (sbyte)radioStatusMessage.Range;
			if (range == this._prevVal)
			{
				return;
			}
			if (range < 0)
			{
				this._playback.gameObject.SetActive(false);
			}
			else
			{
				this._playback.gameObject.SetActive(true);
				this._playback.RangeId = (int)range;
			}
			this._prevVal = range;
		}

		internal override void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
		{
			base.Initialize(subcontroller, id);
			this._prevVal = sbyte.MinValue;
			this._netId = subcontroller.Model.OwnerHub.netId;
			this._playback.IgnoredNetId = this._netId;
		}

		[SerializeField]
		private SpatializedRadioPlaybackBase _playback;

		private uint _netId;

		private sbyte _prevVal;
	}
}
