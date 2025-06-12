using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079DoorLockReleaser : Scp079KeyAbilityBase
{
	private static string _releaseMessage;

	private Scp079DoorLockChanger _lockChanger;

	private const string ColorFormat = "<color=#ffffff{0}>{1}</color>";

	private const float BlinkRate = 2.8f;

	public override ActionName ActivationKey => ActionName.Scp079UnlockAll;

	public override bool IsReady => true;

	public override bool IsVisible => this._lockChanger.LockedDoor != null;

	public override string AbilityName => $"<color=#ffffff{this.Transparency}>{Scp079DoorLockReleaser._releaseMessage}</color>";

	public override string FailMessage => null;

	private string Transparency
	{
		get
		{
			float f = Time.timeSinceLevelLoad * 2.8f * MathF.PI;
			return Mathf.RoundToInt(Mathf.InverseLerp(-1f, 1f, Mathf.Sin(f)) * 255f).ToString("X2");
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
		Scp079DoorLockReleaser._releaseMessage = Translations.Get(Scp079HudTranslation.ReleaseDoorLock);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this._lockChanger.ServerUnlock();
	}
}
