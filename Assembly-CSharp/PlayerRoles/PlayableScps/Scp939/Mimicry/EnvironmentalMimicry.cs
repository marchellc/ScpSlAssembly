using System;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class EnvironmentalMimicry : StandardSubroutine<Scp939Role>
	{
		public event Action OnSoundPlayed;

		public EnvMimicrySequence[] Sequences { get; private set; }

		public string CooldownText
		{
			get
			{
				return string.Format(Translations.Get<Scp939HudTranslation>(Scp939HudTranslation.EnvMimicryCooldown), ((float)Mathf.RoundToInt(this.Cooldown.Remaining * 10f) / 10f).ToString("0.0"));
			}
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<MimicPointController>(out this._mimicPoint);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
			this._hasSound = false;
			if (this._currentlyPlayed == null)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(this._currentlyPlayed);
			this._currentlyPlayed = null;
		}

		public void ClientSelect(EnvMimicrySequence sequence)
		{
			int num = this.Sequences.Length;
			for (int i = 0; i < num; i++)
			{
				if (!(this.Sequences[i] != sequence))
				{
					this._syncOption = (byte)i;
					base.ClientSendCmd();
					return;
				}
			}
			Debug.LogError("Sequence " + ((sequence == null) ? "null" : sequence.name) + " is not whitelisted!");
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteByte(this._syncOption);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			this._syncOption = reader.ReadByte();
			this.Cooldown.Trigger((double)this._activationCooldown);
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte(this._syncOption);
			this.Cooldown.WriteCooldown(writer);
			writer.WriteInt(global::UnityEngine.Random.Range(int.MinValue, int.MaxValue));
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._syncOption = (byte)((int)reader.ReadByte() % this.Sequences.Length);
			this.Cooldown.ReadCooldown(reader);
			this._currentlyPlayed = global::UnityEngine.Object.Instantiate<EnvMimicrySequence>(this.Sequences[(int)this._syncOption]);
			this._currentlyPlayed.EnqueueAll(reader.ReadInt());
			this._hasSound = true;
			Action onSoundPlayed = this.OnSoundPlayed;
			if (onSoundPlayed == null)
			{
				return;
			}
			onSoundPlayed();
		}

		private void Update()
		{
			if (!this._hasSound || this._currentlyPlayed == null)
			{
				return;
			}
			if (this._currentlyPlayed.UpdateSequence(this._mimicPoint.MimicPointTransform))
			{
				return;
			}
			this._hasSound = false;
			this._currentlyPlayed = null;
			global::UnityEngine.Object.Destroy(this._currentlyPlayed);
		}

		private byte _syncOption;

		private bool _hasSound;

		private EnvMimicrySequence _currentlyPlayed;

		private MimicPointController _mimicPoint;

		[SerializeField]
		private float _activationCooldown;

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();
	}
}
