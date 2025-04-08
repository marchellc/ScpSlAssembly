using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class CameraOvercon : StandardOvercon
	{
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
			FacilityZone zone = this.Target.Room.Zone;
			if (zone != this._prevZone)
			{
				Sprite sprite;
				this.GetZoneOverrides(zone, out sprite, out this._prevOffset);
				this.TargetSprite.sprite = sprite;
				this._prevZone = zone;
			}
			Vector3 vector = target.transform.TransformPoint(this._prevOffset);
			if (newCam.Room == this.Target.Room)
			{
				this._externalIcon.SetActive(isElevator);
				this.Position = vector;
				base.Rescale(newCam);
				return;
			}
			HashSet<DoorVariant> hashSet;
			DoorVariant doorVariant;
			if (DoorVariant.DoorsByRoom.TryGetValue(this.Target.Room, out hashSet) && hashSet.TryGetFirst((DoorVariant x) => x.Rooms.Contains(newCam.Room), out doorVariant))
			{
				this.Position = doorVariant.transform.position + Vector3.up * 3.2f;
				base.Rescale(newCam, 255f);
			}
			else
			{
				this.Position = vector;
				base.Rescale(newCam);
			}
			this._externalIcon.SetActive(true);
		}

		private void GetZoneOverrides(FacilityZone zone, out Sprite icon, out Vector3 offset)
		{
			foreach (CameraOvercon.ZoneOverrride zoneOverrride in this._zoneOverrides)
			{
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
			this.TargetSprite.color = Color.Lerp(StandardOvercon.NormalColor, StandardOvercon.HighlightedColor, this.IsHighlighted ? 1f : ((Scp079ForwardCameraSelector.HighlightedCamera == this.Target) ? 0.3f : 0f));
		}

		private const float ColorSelectorTarget = 0.3f;

		private const float ExternalCamHeight = 3.2f;

		[SerializeField]
		private Sprite _defaultIcon;

		[SerializeField]
		private Vector3 _defaultOffset;

		[SerializeField]
		private CameraOvercon.ZoneOverrride[] _zoneOverrides;

		[SerializeField]
		private GameObject _externalIcon;

		private FacilityZone _prevZone;

		private Vector3 _prevOffset;

		private Vector3 _position;

		[Serializable]
		private struct ZoneOverrride
		{
			public FacilityZone Zone;

			public Sprite Icon;

			public Vector3 Offset;
		}
	}
}
