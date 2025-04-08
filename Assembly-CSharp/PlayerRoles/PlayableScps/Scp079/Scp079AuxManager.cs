using System;
using System.Text;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079AuxManager : StandardSubroutine<Scp079Role>, IScp079LevelUpNotifier
	{
		private ushort Compressed
		{
			get
			{
				return (ushort)Mathf.Min(this.CurrentAuxFloored, 65535);
			}
		}

		private float RegenSpeed
		{
			get
			{
				float num = this._regenerationPerTier[this._tierManager.AccessTierIndex];
				for (int i = 0; i < this._abilitiesCount; i++)
				{
					num *= this._abilities[i].AuxRegenMultiplier;
				}
				return num;
			}
		}

		public float CurrentAux
		{
			get
			{
				return this._aux;
			}
			set
			{
				value = Mathf.Clamp(value, 0f, this.MaxAux);
				if (value == this._aux)
				{
					return;
				}
				this._aux = value;
				this._valueDirty = true;
			}
		}

		public int CurrentAuxFloored
		{
			get
			{
				return Mathf.FloorToInt(this.CurrentAux);
			}
		}

		public float MaxAux
		{
			get
			{
				return this._maxPerTier[this._tierManager.AccessTierIndex];
			}
		}

		private void Start()
		{
			Scp079AuxManager._textEtaFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.EtaTimer);
			Scp079AuxManager._textHigherTierRequired = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.HigherTierRequired);
			Scp079AuxManager._textNewMaxAux = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.AuxPowerLimitIncreased);
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.Regenerate();
			this.SyncValues();
		}

		private void Regenerate()
		{
			this.CurrentAux += Time.deltaTime * this.RegenSpeed;
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prev, PlayerRoleBase cur)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (cur is SpectatorRole)
			{
				base.ServerSendRpc(hub);
			}
		}

		private void SyncValues()
		{
			if (!this._valueDirty)
			{
				return;
			}
			ushort compressed = this.Compressed;
			this._valueDirty = false;
			if (compressed == this._prevSent)
			{
				return;
			}
			this._prevSent = compressed;
			base.ServerSendRpc((ReferenceHub x) => x == base.Owner || base.Owner.IsSpectatedBy(x));
		}

		public string GenerateETA(float requiredAux)
		{
			if (requiredAux > this.MaxAux)
			{
				return Scp079AuxManager._textHigherTierRequired;
			}
			float regenSpeed = this.RegenSpeed;
			if (regenSpeed <= 0f)
			{
				return string.Empty;
			}
			float num = Mathf.Max(0f, requiredAux - this.CurrentAux);
			return this.GenerateCustomETA(Mathf.CeilToInt(num / regenSpeed));
		}

		public string GenerateCustomETA(int secondsRemaining)
		{
			return string.Format(Scp079AuxManager._textEtaFormat, secondsRemaining);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			SubroutineManagerModule subroutineModule = base.CastRole.SubroutineModule;
			subroutineModule.TryGetSubroutine<Scp079TierManager>(out this._tierManager);
			int num = subroutineModule.AllSubroutines.Length;
			this._abilities = new IScp079AuxRegenModifier[num];
			this._abilitiesCount = 0;
			for (int i = 0; i < num; i++)
			{
				IScp079AuxRegenModifier scp079AuxRegenModifier = subroutineModule.AllSubroutines[i] as IScp079AuxRegenModifier;
				if (scp079AuxRegenModifier != null)
				{
					this._abilities[this._abilitiesCount] = scp079AuxRegenModifier;
					this._abilitiesCount++;
				}
			}
			this.CurrentAux = this._maxPerTier[0];
		}

		public override void ResetObject()
		{
			base.ResetObject();
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteUShort(this._prevSent);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (NetworkServer.active)
			{
				return;
			}
			this.CurrentAux = (float)reader.ReadUShort();
		}

		public bool WriteLevelUpNotification(StringBuilder sb, int newLevel)
		{
			sb.AppendFormat(Scp079AuxManager._textNewMaxAux, this._maxPerTier[newLevel]);
			return true;
		}

		[SerializeField]
		private float[] _regenerationPerTier;

		[SerializeField]
		private float[] _maxPerTier;

		private Scp079TierManager _tierManager;

		private IScp079AuxRegenModifier[] _abilities;

		private int _abilitiesCount;

		private float _aux;

		private bool _valueDirty;

		private ushort _prevSent;

		private static string _textEtaFormat;

		private static string _textHigherTierRequired;

		private static string _textNewMaxAux;
	}
}
