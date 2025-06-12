using System;
using AdminToys;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Overcons;

public class CameraOvercon : StandardOvercon
{
	[Serializable]
	private struct ZoneOverrride
	{
		public FacilityZone Zone;

		public Sprite Icon;

		public Vector3 Offset;
	}

	private const float ColorSelectorTarget = 0.3f;

	private const float ExternalCamHeight = 3.2f;

	[SerializeField]
	private Sprite _defaultIcon;

	[SerializeField]
	private Vector3 _defaultOffset;

	[SerializeField]
	private ZoneOverrride[] _zoneOverrides;

	[SerializeField]
	private GameObject _externalIcon;

	private FacilityZone _prevZone;

	private Vector3 _prevOffset;

	private Vector3 _position;

	public Scp079Camera Target { get; private set; }

	public bool IsElevator { get; private set; }

	public Vector3 Position
	{
		get
		{
			return this._position;
		}
		set
		{
			this._position = value;
			base.transform.position = value;
		}
	}

	internal void Setup(Scp079Camera newCam, Scp079Camera target, bool isElevator)
	{
		this.Target = target;
		this.IsElevator = isElevator;
		Scp079CameraToy component = null;
		FacilityZone facilityZone = ((!target.IsToy || !target.TryGetComponent<Scp079CameraToy>(out component)) ? this.Target.Room.Zone : component.ZoneIcon);
		if (facilityZone != this._prevZone)
		{
			this.GetZoneOverrides(facilityZone, out var icon, out this._prevOffset);
			base.TargetSprite.sprite = icon;
			this._prevZone = facilityZone;
		}
		Vector3 position = target.transform.TransformPoint(this._prevOffset);
		if (newCam.Room == this.Target.Room)
		{
			this._externalIcon.SetActive(isElevator);
			if (component != null)
			{
				component.CurrentOvercon = this;
				this.Position = target.transform.position;
			}
			else
			{
				this.Position = position;
			}
			base.Rescale(newCam);
		}
		else
		{
			if (DoorVariant.DoorsByRoom.TryGetValue(this.Target.Room, out var value) && value.TryGetFirst((DoorVariant x) => x.Rooms.Contains(newCam.Room), out var first))
			{
				this.Position = first.transform.position + Vector3.up * 3.2f;
				base.Rescale(newCam, 255f);
			}
			else
			{
				this.Position = position;
				base.Rescale(newCam);
			}
			this._externalIcon.SetActive(value: true);
		}
	}

	private void GetZoneOverrides(FacilityZone zone, out Sprite icon, out Vector3 offset)
	{
		ZoneOverrride[] zoneOverrides = this._zoneOverrides;
		for (int i = 0; i < zoneOverrides.Length; i++)
		{
			ZoneOverrride zoneOverrride = zoneOverrides[i];
			if (zoneOverrride.Zone == zone)
			{
				icon = zoneOverrride.Icon;
				offset = zoneOverrride.Offset;
				return;
			}
		}
		icon = this._defaultIcon;
		offset = this._defaultOffset;
	}

	private void LateUpdate()
	{
		base.TargetSprite.color = Color.Lerp(StandardOvercon.NormalColor, StandardOvercon.HighlightedColor, this.IsHighlighted ? 1f : ((Scp079ForwardCameraSelector.HighlightedCamera == this.Target) ? 0.3f : 0f));
	}
}
