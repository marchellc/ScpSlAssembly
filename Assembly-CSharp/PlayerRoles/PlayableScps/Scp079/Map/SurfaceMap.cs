using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class SurfaceMap : MonoBehaviour, IZoneMap
	{
		public bool Ready { get; private set; }

		public Bounds RectBounds { get; private set; }

		public FacilityZone Zone
		{
			get
			{
				return FacilityZone.Surface;
			}
		}

		private Vector2 WorldspaceToAnchored(Vector3 pos)
		{
			Vector2 sizeDelta = this._parent.sizeDelta;
			return new Vector2(Mathf.InverseLerp(SurfaceMap.BottomLeftWorldspace.x, SurfaceMap.TopRightWorldspace.x, pos.x) * sizeDelta.x, Mathf.InverseLerp(SurfaceMap.BottomLeftWorldspace.z, SurfaceMap.TopRightWorldspace.z, pos.z) * sizeDelta.y);
		}

		public void Generate()
		{
			this._parentImage = this._parent.GetComponent<Image>();
			this._nonExactTransform = base.GetComponentInChildren<LayoutGroup>(true).transform;
			this._zoneLabel.text = Translations.Get<Scp079HudTranslation>(ProceduralZoneMap.ZoneTranslations[FacilityZone.Surface]);
			this._surfaceCameras = new List<Scp079Camera>();
			foreach (Scp079InteractableBase scp079InteractableBase in Scp079InteractableBase.AllInstances)
			{
				Scp079Camera scp079Camera = scp079InteractableBase as Scp079Camera;
				if (scp079Camera != null && scp079InteractableBase.Room.Zone == FacilityZone.Surface)
				{
					this._surfaceCameras.Add(scp079Camera);
				}
			}
			int count = this._surfaceCameras.Count;
			this._icons = new Image[count];
			for (int i = 0; i < count; i++)
			{
				this._icons[i] = global::UnityEngine.Object.Instantiate<Image>(this._template, this._parent);
				this._icons[i].rectTransform.anchoredPosition = this.WorldspaceToAnchored(this._surfaceCameras[i].Position);
			}
			global::UnityEngine.Object.Destroy(this._template);
		}

		public bool TryGetCamera(out Scp079Camera target)
		{
			if (this._targetCam < 0)
			{
				target = null;
				return false;
			}
			target = this._surfaceCameras[this._targetCam];
			return true;
		}

		public bool TryGetCenterTransform(Scp079Camera curCam, out Vector3 center)
		{
			center = Vector3.zero;
			if (curCam.Room.Zone != FacilityZone.Surface)
			{
				return false;
			}
			for (int i = 0; i < this._icons.Length; i++)
			{
				if (!(this._surfaceCameras[i] != curCam))
				{
					center = -this._icons[i].rectTransform.localPosition - this._parent.anchoredPosition;
					return true;
				}
			}
			return false;
		}

		public bool TrySetPlayerIndicator(ReferenceHub ply, RectTransform indicator, bool exact)
		{
			IFpcRole fpcRole = ply.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			Vector3 position = fpcRole.FpcModule.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(position, true);
			if (roomIdentifier == null || roomIdentifier.Zone != FacilityZone.Surface)
			{
				return false;
			}
			indicator.SetParent(exact ? this._indicatorsRoot : this._nonExactTransform);
			indicator.localPosition = Vector3.zero;
			indicator.localScale = Vector3.one;
			if (exact)
			{
				indicator.anchoredPosition = this.WorldspaceToAnchored(position);
				indicator.localEulerAngles = Vector3.back * fpcRole.FpcModule.MouseLook.CurrentHorizontal;
			}
			else
			{
				indicator.localEulerAngles = Vector3.zero;
			}
			return true;
		}

		public void UpdateOpened(Scp079Camera curCam)
		{
			if (!this.Ready && this._lczMap.Ready)
			{
				Bounds rectBounds = this._lczMap.RectBounds;
				Vector2 vector = rectBounds.center - this._spacing - this._parent.sizeDelta / 2f;
				Vector3 vector2 = new Vector3(vector.x - rectBounds.extents.x, vector.y + rectBounds.extents.y, 0f);
				this._parent.localPosition = vector2;
				this.RectBounds = new Bounds(vector2, this._parent.sizeDelta);
				this.Ready = true;
			}
			this._parentImage.color = ((curCam.Room.Zone == FacilityZone.Surface) ? ProceduralZoneMap.CurrentZoneColor : ProceduralZoneMap.OtherZoneColor);
			this._targetCam = -1;
			for (int i = 0; i < this._icons.Length; i++)
			{
				if (this._surfaceCameras[i] == curCam)
				{
					this._icons[i].color = ProceduralZoneMap.CurrentRoomColor;
				}
				else if (this._scalerRoot.InverseTransformPoint(this._icons[i].rectTransform.position).sqrMagnitude < 3500f)
				{
					this._targetCam = i;
					this._icons[i].color = ProceduralZoneMap.HighlightedColor;
				}
				else
				{
					this._icons[i].color = ProceduralZoneMap.CurrentZoneColor;
				}
			}
		}

		private const float ActivationMagSqr = 3500f;

		private static readonly Vector3 BottomLeftWorldspace = new Vector3(-25.168f, 0f, -82.351f);

		private static readonly Vector3 TopRightWorldspace = new Vector3(149.977f, 0f, 33.144f);

		[SerializeField]
		private RectTransform _parent;

		[SerializeField]
		private Image _template;

		[SerializeField]
		private LczMap _lczMap;

		[SerializeField]
		private Vector2 _spacing;

		[SerializeField]
		private TextMeshProUGUI _zoneLabel;

		[SerializeField]
		private RectTransform _scalerRoot;

		[SerializeField]
		private RectTransform _indicatorsRoot;

		private Image[] _icons;

		private Image _parentImage;

		private Transform _nonExactTransform;

		private int _targetCam;

		private List<Scp079Camera> _surfaceCameras;
	}
}
