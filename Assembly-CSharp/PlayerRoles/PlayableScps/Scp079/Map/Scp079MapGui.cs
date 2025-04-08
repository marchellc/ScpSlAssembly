using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using UnityEngine;
using UserSettings.ControlsSettings;

namespace PlayerRoles.PlayableScps.Scp079.Map
{
	public class Scp079MapGui : Scp079GuiElementBase
	{
		public static Scp079Camera HighlightedCamera { get; private set; }

		public static Vector3 SyncVars { get; internal set; }

		private void Update()
		{
			bool isOpen = Scp079ToggleMenuAbilityBase<Scp079MapToggler>.IsOpen;
			float num = Time.deltaTime * (float)(isOpen ? 1 : (-1));
			this._animValue = Mathf.Clamp(this._animValue + num, 0f, this._animDuration);
			if (isOpen)
			{
				if (!this._prevOpen)
				{
					this.OnOpened();
				}
				this.UpdateOpen();
			}
			else if (this._prevOpen)
			{
				Scp079MapGui.HighlightedCamera = null;
			}
			this._prevOpen = isOpen;
			if (this._prevAnimVal == this._animValue)
			{
				return;
			}
			this.EvaluateAll(isOpen, this._animValue);
			this._prevAnimVal = this._animValue;
		}

		private void OnOpened()
		{
			if (!Scp079Role.LocalInstanceActive)
			{
				this._moverPosition = Scp079MapGui.SyncVars;
				this._mapScaler.localScale = Vector3.one * Scp079MapGui.SyncVars.z;
				return;
			}
			this._zoom = 1f;
			this._mapScaler.localScale = Vector3.one;
			Scp079Camera currentCamera = this._curCamSync.CurrentCamera;
			MonoBehaviour[] zoneMaps = this._zoneMaps;
			for (int i = 0; i < zoneMaps.Length; i++)
			{
				Vector3 vector;
				if (((IZoneMap)zoneMaps[i]).TryGetCenterTransform(currentCamera, out vector))
				{
					this._prevOffset = vector;
					break;
				}
			}
			this._mapMover.anchoredPosition = this._prevOffset;
			this._moverPosition = this._mapMover.localPosition;
		}

		private void UpdateOpen()
		{
			Scp079MapGui.HighlightedCamera = null;
			bool flag = !Scp079Role.LocalInstanceActive;
			bool flag2 = !Cursor.visible && !flag;
			bool visible = Scp079ToggleMenuAbilityBase<Scp079ScannerMenuToggler>.Visible;
			if (flag2 && !visible)
			{
				float axisRaw = Input.GetAxisRaw("Mouse ScrollWheel");
				this._zoom = Mathf.Clamp(this._zoom + axisRaw, 0.3f, 2.1f);
				Vector2 vector = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
				this._moverPosition -= 30f * SensitivitySettings.SensMultiplier * vector / this._zoom;
				Scp079MapGui.SyncVars = new Vector3(this._moverPosition.x, this._moverPosition.y, this._zoom);
			}
			if (flag)
			{
				this._zoom = Scp079MapGui.SyncVars.z;
				this._mapScaler.localScale = Vector3.one * this._zoom;
				this._moverPosition = Vector3.Lerp(this._moverPosition, Scp079MapGui.SyncVars, Time.deltaTime * 15f);
				this._mapMover.localPosition = this._moverPosition;
			}
			float animInterpolant = Scp079ScannerGui.AnimInterpolant;
			this._mapScaler.localScale = Vector3.one * Mathf.Lerp(this._zoom, Scp079ScannerGui.MapZoom, animInterpolant);
			this._mapMover.localPosition = Vector3.Lerp(this._moverPosition, Scp079ScannerGui.MapPos, animInterpolant);
			MonoBehaviour[] array;
			if (!visible && (flag2 || flag))
			{
				array = this._zoneMaps;
				for (int i = 0; i < array.Length; i++)
				{
					Scp079Camera scp079Camera;
					if (((IZoneMap)array[i]).TryGetCamera(out scp079Camera))
					{
						Scp079MapGui.HighlightedCamera = scp079Camera;
					}
				}
			}
			Scp079Camera currentCamera = this._curCamSync.CurrentCamera;
			array = this._zoneMaps;
			for (int i = 0; i < array.Length; i++)
			{
				((IZoneMap)array[i]).UpdateOpened(currentCamera);
			}
		}

		private void EvaluateAll(bool open, float val)
		{
			Scp079MapGui.MapAnimation mapAnimation = (open ? this._openAnim : this._closeAnim);
			this._background.alpha = mapAnimation.Background.Evaluate(val);
			this._compressor.alpha = mapAnimation.Compressor.Evaluate(val);
			this._scalable.localScale = new Vector3(mapAnimation.Horizontal.Evaluate(val), mapAnimation.Vertical.Evaluate(val), 1f);
		}

		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
			MonoBehaviour[] zoneMaps = this._zoneMaps;
			for (int i = 0; i < zoneMaps.Length; i++)
			{
				((IZoneMap)zoneMaps[i]).Generate();
			}
		}

		[SerializeField]
		private float _animDuration;

		[SerializeField]
		private CanvasGroup _background;

		[SerializeField]
		private CanvasGroup _compressor;

		[SerializeField]
		private RectTransform _scalable;

		[SerializeField]
		private Scp079MapGui.MapAnimation _closeAnim;

		[SerializeField]
		private Scp079MapGui.MapAnimation _openAnim;

		[SerializeField]
		private MonoBehaviour[] _zoneMaps;

		[SerializeField]
		private RectTransform _mapMover;

		[SerializeField]
		private RectTransform _mapScaler;

		private float _animValue;

		private float _prevAnimVal;

		private float _zoom;

		private bool _prevOpen;

		private Scp079CurrentCameraSync _curCamSync;

		private Vector3 _prevOffset;

		private Vector3 _moverPosition;

		private const string AxisX = "Mouse X";

		private const string AxisY = "Mouse Y";

		private const string AxisScroll = "Mouse ScrollWheel";

		private const float MouseSensitivity = 30f;

		private const float SpectatorLerp = 15f;

		[Serializable]
		private struct MapAnimation
		{
			public AnimationCurve Background;

			public AnimationCurve Horizontal;

			public AnimationCurve Vertical;

			public AnimationCurve Compressor;
		}
	}
}
