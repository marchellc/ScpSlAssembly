using System;
using MapGeneration;
using Mirror;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ScannerZoneSelector : Scp079AbilityBase, IScp079AuxRegenModifier
	{
		public string AuxReductionMessage
		{
			get
			{
				return string.Format(this._regenPauseFormat, this.SelectedZonesCnt);
			}
		}

		public float AuxRegenMultiplier
		{
			get
			{
				return 1f / (float)(1 << this.SelectedZonesCnt);
			}
		}

		public int SelectedZonesCnt
		{
			get
			{
				int num = 0;
				bool[] selectedZones = this._selectedZones;
				for (int i = 0; i < selectedZones.Length; i++)
				{
					if (selectedZones[i])
					{
						num++;
					}
				}
				return num;
			}
		}

		public FacilityZone[] SelectedZones
		{
			get
			{
				int selectedZonesCnt = this.SelectedZonesCnt;
				FacilityZone[] array = new FacilityZone[selectedZonesCnt];
				int num = 0;
				for (int i = 0; i < Scp079ScannerZoneSelector.AllZones.Length; i++)
				{
					if (this._selectedZones[i])
					{
						array[num] = Scp079ScannerZoneSelector.AllZones[i];
						if (++num == selectedZonesCnt)
						{
							break;
						}
					}
				}
				return array;
			}
		}

		private int GetZoneIndex(FacilityZone zone)
		{
			for (int i = 0; i < Scp079ScannerZoneSelector.AllZones.Length; i++)
			{
				if (Scp079ScannerZoneSelector.AllZones[i] == zone)
				{
					return i;
				}
			}
			throw new ArgumentOutOfRangeException(string.Format("Zone {0} is not a valid value of enum type {1}", zone, typeof(FacilityZone).FullName));
		}

		protected override void Awake()
		{
			base.Awake();
			this._regenPauseFormat = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.ScannerAuxPause);
		}

		public bool GetZoneStatus(FacilityZone zone)
		{
			return this._selectedZones[this.GetZoneIndex(zone)];
		}

		public void ToggleZoneStatus(FacilityZone zone)
		{
			bool[] selectedZones = this._selectedZones;
			int zoneIndex = this.GetZoneIndex(zone);
			selectedZones[zoneIndex] = !selectedZones[zoneIndex];
			base.ClientSendCmd();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteBoolArray(this._selectedZones);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			reader.ReadBoolArray(this._selectedZones);
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBoolArray(this._selectedZones);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (base.Role.IsLocalPlayer)
			{
				return;
			}
			reader.ReadBoolArray(this._selectedZones);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			Array.Clear(this._selectedZones, 0, this._selectedZones.Length);
		}

		private static readonly FacilityZone[] AllZones = EnumUtils<FacilityZone>.Values;

		private readonly bool[] _selectedZones = new bool[Scp079ScannerZoneSelector.AllZones.Length];

		private string _regenPauseFormat;
	}
}
