using System;
using MapGeneration;
using Mirror;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079ScannerZoneSelector : Scp079AbilityBase, IScp079AuxRegenModifier
{
	private static readonly FacilityZone[] AllZones = EnumUtils<FacilityZone>.Values;

	private readonly bool[] _selectedZones = new bool[Scp079ScannerZoneSelector.AllZones.Length];

	private string _regenPauseFormat;

	public string AuxReductionMessage => string.Format(this._regenPauseFormat, this.SelectedZonesCnt);

	public float AuxRegenMultiplier => 1f / (float)(1 << this.SelectedZonesCnt);

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
		throw new ArgumentOutOfRangeException($"Zone {zone} is not a valid value of enum type {typeof(FacilityZone).FullName}");
	}

	protected override void Awake()
	{
		base.Awake();
		this._regenPauseFormat = Translations.Get(Scp079HudTranslation.ScannerAuxPause);
	}

	public bool GetZoneStatus(FacilityZone zone)
	{
		return this._selectedZones[this.GetZoneIndex(zone)];
	}

	public void ToggleZoneStatus(FacilityZone zone)
	{
		ref bool reference = ref this._selectedZones[this.GetZoneIndex(zone)];
		reference = !reference;
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
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBoolArray(this._selectedZones);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!base.Role.IsLocalPlayer)
		{
			reader.ReadBoolArray(this._selectedZones);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		Array.Clear(this._selectedZones, 0, this._selectedZones.Length);
	}
}
