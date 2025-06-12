using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using MapGeneration;
using Mirror;
using NorthwoodLib.Pools;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079ScannerSequence
{
	private enum ScanSequenceStep
	{
		Init,
		CountingDown,
		ScanningFindNewTarget,
		ScanningFailedCooldown,
		ScanningUpdateTarget
	}

	private enum TrackerMessage
	{
		None,
		ScannerDisabled,
		ScanTimeSync,
		ScanNoResults,
		ScanSuccessful
	}

	private const float TotalCountdownTime = 20f;

	private const float AddZonesDuringCooldownPenalty = 4f;

	private const float ScanningTime = 3.2f;

	private const float ScannedEffectDuration = 7.5f;

	private readonly Scp079ScannerMenuToggler _menuToggler;

	private readonly Scp079ScannerZoneSelector _zoneSelector;

	private readonly Scp079ScannerTeamFilterSelector _teamSelector;

	private readonly Scp079ScannerTracker _tracker;

	private int _prevZonesCnt;

	private int _detectedPlayer;

	private double _nextScanTime;

	private double _scanCompleteTime;

	private bool _wasEnabled;

	private Team[] _teamsToDetect;

	private FacilityZone[] _zonesToDetect;

	private ScanSequenceStep _curStep;

	private TrackerMessage _rpcToSend;

	public bool SequencePaused { get; private set; }

	public float RemainingTime => (float)(this._nextScanTime - NetworkTime.time);

	private bool ScanningPossible
	{
		get
		{
			if (this._menuToggler.IsUnlocked && this._zoneSelector.SelectedZonesCnt > 0)
			{
				return this._teamSelector.AnySelected;
			}
			return false;
		}
	}

	private Scp079ScannerTrackedPlayer[] TrackedPlayers => this._tracker.TrackedPlayers;

	private TrackerMessage UpdateSequence()
	{
		double time = NetworkTime.time;
		switch (this._curStep)
		{
		case ScanSequenceStep.Init:
			if (!this.ScanningPossible)
			{
				if (!this._wasEnabled)
				{
					return TrackerMessage.None;
				}
				this._wasEnabled = false;
				return TrackerMessage.ScannerDisabled;
			}
			this._nextScanTime = time + 20.0;
			this._prevZonesCnt = this._zoneSelector.SelectedZonesCnt;
			this._wasEnabled = true;
			this._curStep = ScanSequenceStep.CountingDown;
			return TrackerMessage.ScanTimeSync;
		case ScanSequenceStep.CountingDown:
		{
			if (time >= this._nextScanTime)
			{
				this._teamsToDetect = this._teamSelector.SelectedTeams;
				this._zonesToDetect = this._zoneSelector.SelectedZones;
				this._curStep = ScanSequenceStep.ScanningFindNewTarget;
				return TrackerMessage.None;
			}
			if (!this.ScanningPossible)
			{
				this._curStep = ScanSequenceStep.Init;
				return TrackerMessage.None;
			}
			int selectedZonesCnt = this._zoneSelector.SelectedZonesCnt;
			int prevZonesCnt = this._prevZonesCnt;
			this._prevZonesCnt = selectedZonesCnt;
			if (selectedZonesCnt <= prevZonesCnt)
			{
				return TrackerMessage.None;
			}
			this._nextScanTime = Math.Min(time + 20.0, this._nextScanTime + 4.0);
			return TrackerMessage.ScanTimeSync;
		}
		case ScanSequenceStep.ScanningFindNewTarget:
		{
			List<int> list = ListPool<int>.Shared.Rent();
			for (int i = 0; i < this.TrackedPlayers.Length; i++)
			{
				Scp079ScannerTrackedPlayer scp079ScannerTrackedPlayer2 = this.TrackedPlayers[i];
				if (scp079ScannerTrackedPlayer2 != null && scp079ScannerTrackedPlayer2.IsCamping && this._zonesToDetect.Contains(scp079ScannerTrackedPlayer2.LastZone) && this._teamsToDetect.Contains(scp079ScannerTrackedPlayer2.Hub.GetTeam()))
				{
					list.Add(i);
				}
			}
			this._scanCompleteTime = time + 3.200000047683716;
			if (list.Count == 0)
			{
				this._curStep = ScanSequenceStep.ScanningFailedCooldown;
			}
			else
			{
				this._detectedPlayer = list.RandomItem();
				ListPool<int>.Shared.Return(list);
				this.TrackedPlayers[this._detectedPlayer].Hub.playerEffectsController.EnableEffect<Scanned>(7.5f);
				this._curStep = ScanSequenceStep.ScanningUpdateTarget;
			}
			return TrackerMessage.None;
		}
		case ScanSequenceStep.ScanningFailedCooldown:
			if (time < this._scanCompleteTime)
			{
				return TrackerMessage.None;
			}
			this._curStep = ScanSequenceStep.Init;
			return TrackerMessage.ScanNoResults;
		case ScanSequenceStep.ScanningUpdateTarget:
		{
			Scp079ScannerTrackedPlayer scp079ScannerTrackedPlayer = this.TrackedPlayers[this._detectedPlayer];
			if (scp079ScannerTrackedPlayer != null && scp079ScannerTrackedPlayer.LastZone != FacilityZone.Other)
			{
				if (time < this._scanCompleteTime)
				{
					return TrackerMessage.None;
				}
				this._curStep = ScanSequenceStep.Init;
				return TrackerMessage.ScanSuccessful;
			}
			goto case ScanSequenceStep.ScanningFindNewTarget;
		}
		default:
			return TrackerMessage.None;
		}
	}

	public Scp079ScannerSequence(Scp079Role role)
	{
		this.SequencePaused = true;
		role.SubroutineModule.TryGetSubroutine<Scp079ScannerMenuToggler>(out this._menuToggler);
		role.SubroutineModule.TryGetSubroutine<Scp079ScannerZoneSelector>(out this._zoneSelector);
		role.SubroutineModule.TryGetSubroutine<Scp079ScannerTeamFilterSelector>(out this._teamSelector);
		role.SubroutineModule.TryGetSubroutine<Scp079ScannerTracker>(out this._tracker);
	}

	public void ServerUpdate(out bool rpcRequested)
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("Breach Scanner sequence can only be updated by the server!");
		}
		TrackerMessage trackerMessage = this.UpdateSequence();
		rpcRequested = trackerMessage != TrackerMessage.None;
		if (rpcRequested)
		{
			this._rpcToSend = trackerMessage;
		}
	}

	public void WriteRpc(NetworkWriter writer)
	{
		writer.WriteByte((byte)this._rpcToSend);
		switch (this._rpcToSend)
		{
		case TrackerMessage.ScanNoResults:
			writer.WriteByte((byte)(this.ScanningPossible ? 20f : 0f));
			break;
		case TrackerMessage.ScanTimeSync:
			writer.WriteDouble(this._nextScanTime);
			break;
		case TrackerMessage.ScanSuccessful:
		{
			Scp079ScannerTrackedPlayer scp079ScannerTrackedPlayer = this.TrackedPlayers[this._detectedPlayer];
			Vector3 vector = RoomUtils.CoordsToCenterPos(RoomUtils.PositionToCoords(scp079ScannerTrackedPlayer.PlyPos));
			Vector3 targetPos = new Vector3(vector.x, scp079ScannerTrackedPlayer.PlyPos.y, vector.z);
			writer.WriteReferenceHub(scp079ScannerTrackedPlayer.Hub);
			writer.WriteByte(20);
			writer.WriteRelativePosition(new RelativePosition(targetPos));
			break;
		}
		}
	}

	public void ReadRpc(NetworkReader reader)
	{
		switch ((TrackerMessage)reader.ReadByte())
		{
		case TrackerMessage.ScannerDisabled:
			this.SequencePaused = true;
			break;
		case TrackerMessage.ScanTimeSync:
			this.SequencePaused = false;
			this._nextScanTime = reader.ReadDouble();
			break;
		case TrackerMessage.ScanNoResults:
			this._tracker.ClientProcessScanResult(null, reader.ReadByte(), null);
			break;
		case TrackerMessage.ScanSuccessful:
			this._tracker.ClientProcessScanResult(reader.ReadReferenceHub(), reader.ReadByte(), reader);
			break;
		}
	}
}
