using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079ZoomGui : Scp079GuiElementBase
	{
		internal override void Init(Scp079Role role, ReferenceHub owner)
		{
			base.Init(role, owner);
			this._format = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.Zoom);
			role.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		}

		private void Update()
		{
			Scp079Camera scp079Camera;
			if (!this._curCamSync.TryGetCurrentCamera(out scp079Camera))
			{
				return;
			}
			CameraZoomAxis zoomAxis = scp079Camera.ZoomAxis;
			float num = Mathf.InverseLerp(zoomAxis.MinValue, zoomAxis.MaxValue, zoomAxis.CurValue);
			float num2 = Mathf.Round(10f * zoomAxis.CurrentZoom) / 10f;
			if (num <= 0f)
			{
				this._root.SetActive(false);
				return;
			}
			this._slider.localPosition = Vector2.Lerp(this._minPos, this._maxPos, num);
			this._text.text = string.Format(this._format, num2.ToString("0.0"));
			this._root.SetActive(true);
		}

		private const float RoundingAccuracy = 10f;

		private Scp079CurrentCameraSync _curCamSync;

		private string _format;

		[SerializeField]
		private Vector2 _minPos;

		[SerializeField]
		private Vector2 _maxPos;

		[SerializeField]
		private RectTransform _slider;

		[SerializeField]
		private TextMeshProUGUI _text;

		[SerializeField]
		private GameObject _root;
	}
}
