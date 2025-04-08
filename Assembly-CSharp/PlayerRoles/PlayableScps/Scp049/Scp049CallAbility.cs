using System;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp049
{
	public class Scp049CallAbility : KeySubroutine<Scp049Role>
	{
		public bool IsMarkerShown
		{
			get
			{
				return !this.Duration.IsReady && (!NetworkServer.active || this._serverTriggered);
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Reload;
			}
		}

		private void ServerRefreshDuration()
		{
			if (!this._serverTriggered || !this.Duration.IsReady)
			{
				return;
			}
			this.Cooldown.Trigger(60.0);
			this._serverTriggered = false;
			base.ServerSendRpc(true);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			if (this._serverTriggered || !this.Cooldown.IsReady)
			{
				return;
			}
			Scp049UsingDoctorsCallEventArgs scp049UsingDoctorsCallEventArgs = new Scp049UsingDoctorsCallEventArgs(base.Owner);
			Scp049Events.OnUsingDoctorsCall(scp049UsingDoctorsCallEventArgs);
			if (!scp049UsingDoctorsCallEventArgs.IsAllowed)
			{
				return;
			}
			this.Duration.Trigger(20.0);
			this._serverTriggered = true;
			base.ServerSendRpc(true);
			Scp049Events.OnUsedDoctorsCall(new Scp049UsedDoctorsCallEventArgs(base.Owner));
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			this.Cooldown.WriteCooldown(writer);
			this.Duration.WriteCooldown(writer);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			this.Cooldown.ReadCooldown(reader);
			this.Duration.ReadCooldown(reader);
			if (this.Cooldown.Remaining >= 60f)
			{
				this.AbilityAudio(false);
				return;
			}
			if (this.Duration.Remaining >= 20f)
			{
				this.AbilityAudio(true);
			}
		}

		private void AbilityAudio(bool start)
		{
		}

		protected override void Update()
		{
			base.Update();
			if (!NetworkServer.active)
			{
				return;
			}
			this.ServerRefreshDuration();
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this.Cooldown.IsReady || !this.Duration.IsReady)
			{
				return;
			}
			base.ClientSendCmd();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
			this.Duration.Clear();
			this._serverTriggered = false;
		}

		private const float BaseCooldown = 60f;

		private const float EffectDuration = 20f;

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();

		public readonly AbilityCooldown Duration = new AbilityCooldown();

		private bool _serverTriggered;

		public AbilityHud CallAbilityHUD;
	}
}
