using System;
using System.Text;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079SimpleNotification : IScp079Notification
	{
		public Scp079SimpleNotification(string targetContent, bool mute = false)
		{
			this._mute = mute;
			this._writtenText = new StringBuilder();
			this._targetContent = targetContent;
			this._length = this._targetContent.Length;
			this._startTime = this.CurrentTime;
			this._endTime = this._startTime + 4.2f + 0.05f * (float)this._length;
			Scp079NotificationManager.TryGetTextHeight(targetContent, out this._totalHeight);
		}

		private float CurrentTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		private float Elapsed
		{
			get
			{
				return this.CurrentTime - this._startTime;
			}
		}

		protected virtual StringBuilder WrittenText
		{
			get
			{
				return this._writtenText;
			}
		}

		public virtual float Opacity
		{
			get
			{
				return Mathf.Min(1f, 1f - (this.CurrentTime - this._endTime) / 0.18f);
			}
		}

		public float Height
		{
			get
			{
				float num = ((this.Opacity > 0f) ? (this.Elapsed / 0.08f) : (1f + this.Opacity));
				return Mathf.Lerp(-5f, this._totalHeight, num);
			}
		}

		public string DisplayedText
		{
			get
			{
				this.WriteLetters();
				return this.WrittenText.ToString();
			}
		}

		public NotificationSound Sound
		{
			get
			{
				int num = Mathf.CeilToInt((float)(this._totalWritten - this._lettersOffset) * 0.2f);
				if (num <= this._prevPlayed)
				{
					return NotificationSound.None;
				}
				this._prevPlayed = num;
				if (!this._mute)
				{
					return NotificationSound.Standard;
				}
				return NotificationSound.None;
			}
		}

		public virtual bool Delete
		{
			get
			{
				return this.Opacity < -1f;
			}
		}

		private void WriteLetters()
		{
			int num = Mathf.RoundToInt((this.Elapsed - 0.08f) * 100f) + this._lettersOffset;
			if (num <= 0)
			{
				return;
			}
			bool flag = false;
			while (this._totalWritten < this._length && (this._totalWritten < num || flag))
			{
				char c = this._targetContent[this._totalWritten];
				if (c == '<')
				{
					flag = true;
				}
				else if (c == '>')
				{
					flag = false;
				}
				this._writtenText.Append(c);
				this._totalWritten++;
				if (flag)
				{
					this._lettersOffset++;
				}
			}
		}

		private int _totalWritten;

		private int _prevPlayed;

		private int _lettersOffset;

		private readonly bool _mute;

		private readonly StringBuilder _writtenText;

		private readonly string _targetContent;

		private readonly int _length;

		private readonly float _totalHeight;

		private readonly float _startTime;

		private readonly float _endTime;

		private const float InitialSize = -5f;

		private const float LettersPerSecond = 100f;

		private const float SoundRateRatio = 0.2f;

		private const float FadeInTime = 0.08f;

		private const float AbsoluteDuration = 4.2f;

		private const float PerLetterDuration = 0.05f;

		protected const float FadeOutDuration = 0.18f;
	}
}
