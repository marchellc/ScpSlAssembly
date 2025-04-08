using System;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Reveal : KeySubroutine<Scp3114Role>
	{
		public static event Action OnRevealFail;

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Reload;
			}
		}

		protected override bool KeyPressable
		{
			get
			{
				return base.KeyPressable && base.CastRole.Disguised && this._holdTimer < 0.65f;
			}
		}

		protected override void Update()
		{
			base.Update();
			if (!base.Role.IsLocalPlayer)
			{
				return;
			}
			if (this.IsKeyHeld && base.CastRole.Disguised)
			{
				this._holdTimer += Time.deltaTime;
				return;
			}
			this._holdTimer = 0f;
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			if (!base.CastRole.Disguised)
			{
				return;
			}
			if (this._holdTimer >= 0.65f)
			{
				base.ClientSendCmd();
				return;
			}
			Action onRevealFail = Scp3114Reveal.OnRevealFail;
			if (onRevealFail == null)
			{
				return;
			}
			onRevealFail();
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			base.CastRole.Disguised = false;
		}

		private const float HoldDuration = 0.65f;

		public const ActionName RevealKey = ActionName.Reload;

		private float _holdTimer;
	}
}
