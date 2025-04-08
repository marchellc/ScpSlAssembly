using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using VoiceChat;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryWaveform : UiWaveformVisualizer, IDragHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler
	{
		private float StopwatchElapsed
		{
			get
			{
				return (float)(this._playbackSw.Elapsed.TotalSeconds + this._startPlaybackTimeOffset);
			}
		}

		public bool IsPlaying
		{
			get
			{
				return this._playbackSw.IsRunning && (double)this.StopwatchElapsed < this._playbackMaxDuration;
			}
		}

		public void StartPlayback(int totalLengthSamples, out int startSample, out int lengthSamples)
		{
			if (MimicryWaveform._lastWaveform != null)
			{
				MimicryWaveform._lastWaveform.StopPlayback();
			}
			MimicryWaveform._lastWaveform = this;
			this._playbackTotalDuration = (double)this.SamplesToSeconds(totalLengthSamples);
			if (this._isSet)
			{
				float num;
				float num2;
				this.GetStartStop(out num, out num2);
				startSample = (int)(num * (float)totalLengthSamples);
				lengthSamples = (int)(num2 * (float)totalLengthSamples);
				this._startPlaybackTimeOffset = (double)this.SamplesToSeconds(startSample);
				this._playbackMaxDuration = (double)this.SamplesToSeconds(lengthSamples);
				lengthSamples -= startSample;
			}
			else
			{
				startSample = 0;
				lengthSamples = totalLengthSamples;
				this._startPlaybackTimeOffset = 0.0;
				this._playbackMaxDuration = this._playbackTotalDuration;
			}
			this._playbackSw.Start();
		}

		public void StopPlayback()
		{
			this._playbackSw.Reset();
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!this._isDragging)
			{
				return;
			}
			if (this.TryGetTime(eventData.position, out this._endDragTime))
			{
				return;
			}
			this.Apply();
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!this._isDragging)
			{
				return;
			}
			this.Apply();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			this._isDragging = this.TryGetTime(eventData.position, out this._beginDragTime);
			this._isSet = false;
			this._endDragTime = this._beginDragTime;
		}

		private void Apply()
		{
			float num = Mathf.Abs(this._beginDragTime - this._endDragTime);
			this._isSet = num > this._minCoverage && num < this._maxCoverage;
			this._isDragging = false;
		}

		private bool TryGetTime(Vector2 mousePos, out float percent)
		{
			Rect rect = base.rectTransform.rect;
			Vector2 vector = new Vector2(rect.width, rect.height) * MimicryMenuController.ScaleFactor;
			Vector2 vector2 = mousePos - base.rectTransform.position;
			float num = vector2.x / vector.x + 0.5f;
			percent = Mathf.Clamp01(num);
			return Mathf.Abs(vector2.y * 2f) < vector.y && Mathf.Approximately(num, percent);
		}

		private float SamplesToSeconds(int samples)
		{
			return VoiceChatSettings.SampleToDuartionRate * (float)samples;
		}

		private void GetStartStop(out float startTime, out float stopTime)
		{
			bool flag = this._beginDragTime < this._endDragTime;
			startTime = (flag ? this._beginDragTime : this._endDragTime);
			stopTime = (flag ? this._endDragTime : this._beginDragTime);
		}

		private void Update()
		{
			this.UpdateProgressBar();
			this.UpdateTrimIndicator();
		}

		private void UpdateTrimIndicator()
		{
			bool flag = this._isDragging || this._isSet;
			this._trimIndicator.gameObject.SetActive(flag);
			this._progressBarLimiter.gameObject.SetActive(flag);
			if (!flag)
			{
				return;
			}
			float num;
			float num2;
			this.GetStartStop(out num, out num2);
			float width = base.rectTransform.rect.width;
			this._trimIndicator.offsetMin = num * width * Vector2.right - this._trimmerOffset;
			this._trimIndicator.offsetMax = (1f - num2) * width * Vector2.left + this._trimmerOffset;
			this._progressBarLimiter.localScale = new Vector3(1f - num2, 1f, 1f);
		}

		private void UpdateProgressBar()
		{
			double num = (this.IsPlaying ? ((double)this.StopwatchElapsed / this._playbackTotalDuration) : 1.0);
			this._waveformProgressBar.localScale = new Vector3((float)num, 1f, 1f);
		}

		private static MimicryWaveform _lastWaveform;

		private readonly Stopwatch _playbackSw = new Stopwatch();

		[SerializeField]
		private RectTransform _trimIndicator;

		[SerializeField]
		private float _minCoverage;

		[SerializeField]
		private float _maxCoverage;

		[SerializeField]
		private Vector2 _trimmerOffset;

		[SerializeField]
		private RectTransform _waveformProgressBar;

		[SerializeField]
		private RectTransform _progressBarLimiter;

		private bool _isSet;

		private bool _isDragging;

		private float _beginDragTime;

		private float _endDragTime;

		private double _startPlaybackTimeOffset;

		private double _playbackTotalDuration;

		private double _playbackMaxDuration;
	}
}
