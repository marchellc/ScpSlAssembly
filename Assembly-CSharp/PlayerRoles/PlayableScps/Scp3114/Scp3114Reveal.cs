using System;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Reveal : KeySubroutine<Scp3114Role>
{
	private const float HoldDuration = 0.65f;

	public const ActionName RevealKey = ActionName.Reload;

	private float _holdTimer;

	protected override ActionName TargetKey => ActionName.Reload;

	protected override bool KeyPressable
	{
		get
		{
			if (base.KeyPressable && base.CastRole.Disguised)
			{
				return this._holdTimer < 0.65f;
			}
			return false;
		}
	}

	public static event Action OnRevealFail;

	protected override void Update()
	{
		base.Update();
		if (base.Role.IsLocalPlayer)
		{
			if (this.IsKeyHeld && base.CastRole.Disguised)
			{
				this._holdTimer += Time.deltaTime;
			}
			else
			{
				this._holdTimer = 0f;
			}
		}
	}

	protected override void OnKeyUp()
	{
		base.OnKeyUp();
		if (base.CastRole.Disguised)
		{
			if (this._holdTimer >= 0.65f)
			{
				base.ClientSendCmd();
			}
			else
			{
				Scp3114Reveal.OnRevealFail?.Invoke();
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		base.CastRole.Disguised = false;
	}
}
