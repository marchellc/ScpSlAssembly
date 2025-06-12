using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.Filmmaker;

public class FilmmakerKeyframesRenderer : MonoBehaviour
{
	[Serializable]
	private class Track
	{
		public Color TrackColor;

		public Transform Parent;

		private int _instanceCount;

		private readonly List<RectTransform> _instancesTr = new List<RectTransform>();

		private readonly List<Button> _instancesButtons = new List<Button>();

		private void EnsureIndex(int index, GameObject template)
		{
			for (int i = this._instanceCount; i <= index; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(template, this.Parent);
				gameObject.GetComponentInChildren<Image>().color = this.TrackColor;
				this._instancesTr.Add(gameObject.GetComponent<RectTransform>());
				this._instancesButtons.Add(gameObject.GetComponentInChildren<Button>());
				this._instanceCount++;
			}
		}

		public void GetInstance(int index, GameObject template, out Button button, out RectTransform transform)
		{
			this.EnsureIndex(index, template);
			button = this._instancesButtons[index];
			transform = this._instancesTr[index];
		}

		public void DisableRest(int firstIndex)
		{
			for (int i = firstIndex; i < this._instanceCount; i++)
			{
				this._instancesTr[i].gameObject.SetActive(value: false);
			}
		}
	}

	[SerializeField]
	private float _leftMargin;

	[SerializeField]
	private float _minTimelineWidth;

	[SerializeField]
	private GameObject _keyframeTemplate;

	[SerializeField]
	private Track _posTrack;

	[SerializeField]
	private Track _rotTrack;

	[SerializeField]
	private Track _zoomTrack;

	[SerializeField]
	private RectTransform _scalableTransform;

	[SerializeField]
	private RawImage _secondsIndicator;

	[SerializeField]
	private RawImage _framesIndicator;

	[SerializeField]
	private float _secondsOnTimeline;

	[SerializeField]
	private List<TextMeshProUGUI> _gridIndicators;

	[SerializeField]
	private RectTransform _timeIndicator;

	[SerializeField]
	private RectTransform _setTimeArea;

	[SerializeField]
	private Scrollbar _zoomSlider;

	[SerializeField]
	private Scrollbar _offsetSlider;

	[SerializeField]
	private TextMeshProUGUI _selectionInfo;

	[SerializeField]
	private Button _removeButton;

	[SerializeField]
	private TMP_Dropdown _specificTransitionMode;

	[SerializeField]
	private TMP_Dropdown _defaultTransitionMode;

	[SerializeField]
	private GameObject _detailsGroup;

	private float _frameToNodePosition;

	private float _secondsToNodePosition;

	private float _timelineSize;

	private bool _draggingTime;

	private Canvas _canvas;

	public static event Action OnTimeSet;

	private void Awake()
	{
		this._canvas = base.GetComponentInParent<Canvas>();
		this.PrepDropdown(this._specificTransitionMode);
		this.PrepDropdown(this._defaultTransitionMode);
	}

	private void PrepDropdown(TMP_Dropdown dd)
	{
		dd.ClearOptions();
		for (int i = 0; i <= 3; i++)
		{
			FilmmakerBlendPreset filmmakerBlendPreset = (FilmmakerBlendPreset)i;
			string text = filmmakerBlendPreset.ToString();
			dd.options.Add(new TMP_Dropdown.OptionData(text));
		}
	}

	private void Update()
	{
		this.UpdateTimelineScaleAndOffset();
		this.UpdateGrid();
		this.UpdateTime();
		this.UpdateTrack(this._posTrack, FilmmakerTimelineManager.PositionTrack);
		this.UpdateTrack(this._rotTrack, FilmmakerTimelineManager.RotationTrack);
		this.UpdateTrack(this._zoomTrack, FilmmakerTimelineManager.ZoomTrack);
		this._detailsGroup.SetActive(!string.IsNullOrEmpty(this._selectionInfo.text));
	}

	private void UpdateTimelineScaleAndOffset()
	{
		this._timelineSize = Mathf.Lerp(this._minTimelineWidth, this._minTimelineWidth * Mathf.Max(1f, this._secondsOnTimeline), this._zoomSlider.value);
		this._scalableTransform.sizeDelta = new Vector2(this._timelineSize, this._scalableTransform.sizeDelta.y);
		this._scalableTransform.anchoredPosition = (this._timelineSize - this._minTimelineWidth) * this._offsetSlider.value * Vector2.left;
	}

	private void UpdateGrid()
	{
		float num = this._secondsOnTimeline * 50f;
		this._secondsIndicator.uvRect = new Rect(0f, 0f, this._secondsOnTimeline, 1f);
		this._framesIndicator.uvRect = new Rect(0f, 0f, num, 1f);
		this._frameToNodePosition = (this._scalableTransform.rect.width - this._leftMargin) / num;
		this._secondsToNodePosition = this._frameToNodePosition * 50f;
		int i;
		for (i = 0; (float)i < this._secondsOnTimeline; i++)
		{
			while (i >= this._gridIndicators.Count)
			{
				TextMeshProUGUI textMeshProUGUI = this._gridIndicators[0];
				this._gridIndicators.Add(UnityEngine.Object.Instantiate(textMeshProUGUI, textMeshProUGUI.transform.parent));
			}
			TextMeshProUGUI textMeshProUGUI2 = this._gridIndicators[i];
			textMeshProUGUI2.text = $" {i}s \t";
			textMeshProUGUI2.enabled = true;
			textMeshProUGUI2.rectTransform.sizeDelta = new Vector2(this._secondsToNodePosition, textMeshProUGUI2.rectTransform.sizeDelta.y);
		}
		while (i < this._gridIndicators.Count)
		{
			this._gridIndicators[i++].enabled = false;
		}
	}

	private void UpdateTrack<T>(Track track, FilmmakerTrack<T> timeline) where T : struct
	{
		int num = timeline.Keyframes.Length;
		for (int i = 0; i < num; i++)
		{
			track.GetInstance(i, this._keyframeTemplate, out var button, out var rectTransform);
			FilmmakerKeyframe<T> kf = timeline.Keyframes[i];
			rectTransform.gameObject.SetActive(value: true);
			rectTransform.anchoredPosition = this._frameToNodePosition * (float)kf.TimeFrames * Vector3.right;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(delegate
			{
				this._selectionInfo.color = track.TrackColor;
				float num2 = (float)kf.TimeFrames / 50f;
				this._selectionInfo.text = $"Time: {num2:0.00}s ({kf.TimeFrames} frames)" + $"\nValue: {kf.Value} ({typeof(T).Name})";
				this._removeButton.onClick.RemoveAllListeners();
				this._removeButton.onClick.AddListener(delegate
				{
					timeline.ClearFrame(kf.TimeFrames);
				});
				this._removeButton.onClick.AddListener(delegate
				{
					this._selectionInfo.text = string.Empty;
				});
				this._specificTransitionMode.SetValueWithoutNotify((int)kf.BlendCurve);
				this._specificTransitionMode.onValueChanged.RemoveAllListeners();
				this._specificTransitionMode.onValueChanged.AddListener(delegate(int val)
				{
					kf.BlendCurve = (FilmmakerBlendPreset)val;
					this._selectionInfo.text = string.Empty;
				});
			});
		}
		track.DisableRest(num);
	}

	private void UpdateTime()
	{
		this._timeIndicator.anchoredPosition = this._secondsToNodePosition * FilmmakerTimelineManager.TimeSeconds * Vector2.right;
		if (this._draggingTime)
		{
			if (!Input.GetKey(KeyCode.Mouse0))
			{
				this._draggingTime = false;
				return;
			}
		}
		else if (!Input.GetKeyDown(KeyCode.Mouse0))
		{
			return;
		}
		Rect rect = this._setTimeArea.rect;
		Vector2 vector = (Input.mousePosition - this._setTimeArea.position) / this._canvas.scaleFactor;
		bool flag = true;
		if (vector.y > 0f || 0f - vector.y > rect.height / 2f)
		{
			flag = false;
			if (!this._draggingTime)
			{
				return;
			}
		}
		float num = (vector.x + rect.width / 2f) / rect.width;
		if (!(num < 0f) && !(num > 1f))
		{
			this._draggingTime |= flag;
			FilmmakerTimelineManager.TimeFrames = Mathf.RoundToInt(num * this._secondsOnTimeline * 50f);
			FilmmakerKeyframesRenderer.OnTimeSet?.Invoke();
		}
	}
}
