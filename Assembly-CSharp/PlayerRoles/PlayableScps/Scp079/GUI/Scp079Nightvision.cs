using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079Nightvision : Scp079GuiElementBase
{
	[Serializable]
	private struct ZoneNightvisionPair
	{
		public Volume PostProcess;

		public FacilityZone Zone;
	}

	[SerializeField]
	private ZoneNightvisionPair[] _pairs;

	private Scp079CurrentCameraSync _curCam;

	private Scp079LostSignalHandler _lostSignal;

	private readonly HashSet<Volume> _volumes = new HashSet<Volume>();

	private readonly Dictionary<FacilityZone, Volume> _zoneTargets = new Dictionary<FacilityZone, Volume>();

	private Volume TargetVolume
	{
		get
		{
			if (this._curCam.CurClientSwitchState != Scp079CurrentCameraSync.ClientSwitchState.None)
			{
				return null;
			}
			if (this._lostSignal.Lost)
			{
				return null;
			}
			Scp079Camera currentCamera = base.Role.CurrentCamera;
			if (currentCamera == null)
			{
				return null;
			}
			if (!RoomLightController.IsInDarkenedRoom(currentCamera.Position))
			{
				return null;
			}
			if (!this._zoneTargets.TryGetValue(currentCamera.Room.Zone, out var value))
			{
				return null;
			}
			return value;
		}
	}

	internal override void Init(Scp079Role role, ReferenceHub owner)
	{
		base.Init(role, owner);
		role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCam);
		role.SubroutineModule.TryGetSubroutine<Scp079LostSignalHandler>(out this._lostSignal);
		this._curCam.OnCameraChanged += UpdateAll;
		ZoneNightvisionPair[] pairs = this._pairs;
		for (int i = 0; i < pairs.Length; i++)
		{
			ZoneNightvisionPair zoneNightvisionPair = pairs[i];
			this._volumes.Add(zoneNightvisionPair.PostProcess);
			this._zoneTargets[zoneNightvisionPair.Zone] = zoneNightvisionPair.PostProcess;
		}
		this.UpdateAll();
	}

	private void OnDestroy()
	{
		if (!(this._curCam == null))
		{
			this._curCam.OnCameraChanged -= UpdateAll;
		}
	}

	private void Update()
	{
		this.UpdateAll();
	}

	private void UpdateAll()
	{
		Volume target = this.TargetVolume;
		this._volumes.ForEach(delegate(Volume x)
		{
			x.enabled = x == target;
		});
	}
}
