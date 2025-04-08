using System;
using System.Text;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using Respawning.NamingRules;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Identity : StandardSubroutine<Scp3114Role>, ICustomNicknameDisplayRole
	{
		private void OnRagdollRemoved(BasicRagdoll ragdoll)
		{
			if (this.CurIdentity.Ragdoll != ragdoll)
			{
				return;
			}
			this.CurIdentity.Status = Scp3114Identity.DisguiseStatus.None;
		}

		private void OnPlayerAdded(ReferenceHub player)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.ServerSendRpc(player);
		}

		private void Update()
		{
			this.UpdateWarningAudio();
			if (!NetworkServer.active)
			{
				return;
			}
			if (this.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active && this.RemainingDuration.IsReady)
			{
				this.CurIdentity.Status = Scp3114Identity.DisguiseStatus.None;
				this.ServerResendIdentity();
			}
		}

		private void UpdateWarningAudio()
		{
			double num = (double)this.RemainingDuration.Remaining;
			bool flag = num < (double)this._warningTimeSeconds && num > 0.0;
			if (this._revealWarningSource.isPlaying)
			{
				if (!flag)
				{
					this._revealWarningSource.Stop();
				}
			}
			else
			{
				if (!flag)
				{
					return;
				}
				this._revealWarningSource.Play();
			}
			this._revealWarningSource.mute = !base.Role.IsLocalPlayer && !base.Owner.IsLocallySpectated();
		}

		private void OnIdentityStatusChanged()
		{
			if (this.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Active)
			{
				this._wasDisguised = true;
				this.RemainingDuration.Trigger((double)this._disguiseDurationSeconds);
				return;
			}
			this.RemainingDuration.Clear();
			if (!this._wasDisguised)
			{
				return;
			}
			this._wasDisguised = false;
			this._revealEffectSources.ForEach(delegate(AudioSource x)
			{
				x.Play();
			});
		}

		protected override void Awake()
		{
			base.Awake();
			this.CurIdentity.OnStatusChanged += this.OnIdentityStatusChanged;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnPlayerAdded));
			RagdollManager.OnRagdollRemoved += this.OnRagdollRemoved;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._wasDisguised = false;
			this.RemainingDuration.Clear();
			this.CurIdentity.Status = Scp3114Identity.DisguiseStatus.None;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(this.OnPlayerAdded));
			RagdollManager.OnRagdollRemoved -= this.OnRagdollRemoved;
		}

		public void WriteNickname(ReferenceHub owner, StringBuilder sb, out Color texColor)
		{
			ReferenceHub referenceHub;
			if (ReferenceHub.TryGetLocalHub(out referenceHub) && !HitboxIdentity.IsEnemy(base.Owner, referenceHub))
			{
				NicknameSync.WriteDefaultInfo(owner, sb, out texColor, null);
				return;
			}
			HumanRole humanRole;
			if (this.CurIdentity.Status != Scp3114Identity.DisguiseStatus.Active || !PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this.CurIdentity.StolenRole, out humanRole))
			{
				texColor = base.CastRole.RoleColor;
				sb.Append(base.CastRole.RoleName);
				return;
			}
			texColor = humanRole.RoleColor;
			RagdollData info = this.CurIdentity.Ragdoll.Info;
			sb.AppendLine(info.Nickname);
			sb.Append(humanRole.RoleName);
			Team team = humanRole.Team;
			UnitNamingRule unitNamingRule;
			if (!NamingRulesManager.TryGetNamingRule(team, out unitNamingRule))
			{
				return;
			}
			string text = NamingRulesManager.ClientFetchReceived(team, (int)this.CurIdentity.UnitNameId);
			unitNamingRule.AppendName(sb, text, info.RoleType, owner.nicknameSync.ShownPlayerInfo);
		}

		public void ServerResendIdentity()
		{
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this.RemainingDuration.WriteCooldown(writer);
			writer.WriteNetworkBehaviour(this.CurIdentity.Ragdoll);
			writer.WriteByte(this.CurIdentity.UnitNameId);
			writer.WriteByte((byte)this.CurIdentity.Status);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (NetworkServer.active)
			{
				return;
			}
			this.RemainingDuration.ReadCooldown(reader);
			this.CurIdentity.Ragdoll = reader.ReadNetworkBehaviour<BasicRagdoll>();
			this.CurIdentity.UnitNameId = reader.ReadByte();
			this.CurIdentity.Status = (Scp3114Identity.DisguiseStatus)reader.ReadByte();
		}

		public readonly Scp3114Identity.StolenIdentity CurIdentity = new Scp3114Identity.StolenIdentity();

		public readonly AbilityCooldown RemainingDuration = new AbilityCooldown();

		private bool _wasDisguised;

		[SerializeField]
		private AudioSource[] _revealEffectSources;

		[SerializeField]
		private AudioSource _revealWarningSource;

		[SerializeField]
		private float _warningTimeSeconds;

		[SerializeField]
		private float _disguiseDurationSeconds;

		public enum DisguiseStatus
		{
			None,
			Equipping,
			Active
		}

		public class StolenIdentity
		{
			public event Action OnStatusChanged;

			public Scp3114Identity.DisguiseStatus Status
			{
				get
				{
					return this._status;
				}
				set
				{
					if (this._status == value)
					{
						return;
					}
					this._status = value;
					Action onStatusChanged = this.OnStatusChanged;
					if (onStatusChanged == null)
					{
						return;
					}
					onStatusChanged();
				}
			}

			public RoleTypeId StolenRole
			{
				get
				{
					if (!(this.Ragdoll == null))
					{
						return this.Ragdoll.Info.RoleType;
					}
					return RoleTypeId.None;
				}
			}

			public void Reset()
			{
				this._status = Scp3114Identity.DisguiseStatus.None;
				this.Ragdoll = null;
				this.UnitNameId = 0;
			}

			private Scp3114Identity.DisguiseStatus _status;

			public BasicRagdoll Ragdoll;

			public byte UnitNameId;
		}
	}
}
