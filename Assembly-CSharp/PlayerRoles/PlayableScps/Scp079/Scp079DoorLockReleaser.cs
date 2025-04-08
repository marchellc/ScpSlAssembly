using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079DoorLockReleaser : Scp079KeyAbilityBase
	{
		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079UnlockAll;
			}
		}

		public override bool IsReady
		{
			get
			{
				return true;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return this._lockChanger.LockedDoor != null;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format("<color=#ffffff{0}>{1}</color>", this.Transparency, Scp079DoorLockReleaser._releaseMessage);
			}
		}

		public override string FailMessage
		{
			get
			{
				return null;
			}
		}

		private string Transparency
		{
			get
			{
				float num = Time.timeSinceLevelLoad * 2.8f * 3.1415927f;
				return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, Mathf.Sin(num)) * 255f).ToString("X2");
			}
		}

		protected override void Trigger()
		{
			base.ClientSendCmd();
		}

		protected override void Start()
		{
			base.Start();
			base.GetSubroutine<Scp079DoorLockChanger>(out this._lockChanger);
			Scp079DoorLockReleaser._releaseMessage = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ReleaseDoorLock);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._lockChanger.ServerUnlock();
		}

		private static string _releaseMessage;

		private Scp079DoorLockChanger _lockChanger;

		private const string ColorFormat = "<color=#ffffff{0}>{1}</color>";

		private const float BlinkRate = 2.8f;
	}
}
