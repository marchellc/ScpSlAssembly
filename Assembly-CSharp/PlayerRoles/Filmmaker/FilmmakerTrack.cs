using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerRoles.Filmmaker
{
	public class FilmmakerTrack<T> : IFilmmakerTrack where T : struct
	{
		public FilmmakerKeyframe<T>[] Keyframes
		{
			get
			{
				FilmmakerKeyframe<T>[] array;
				if ((array = this._ordered) == null)
				{
					array = (this._ordered = this._unordered.OrderBy((FilmmakerKeyframe<T> x) => x.TimeFrames).ToArray<FilmmakerKeyframe<T>>());
				}
				return array;
			}
		}

		public int FirstFrame
		{
			get
			{
				if (this.Keyframes.Length != 0)
				{
					return this.Keyframes[0].TimeFrames;
				}
				return 0;
			}
		}

		public int LastFrame
		{
			get
			{
				if (this.Keyframes.Length != 0)
				{
					FilmmakerKeyframe<T>[] keyframes = this.Keyframes;
					return keyframes[keyframes.Length - 1].TimeFrames;
				}
				return 0;
			}
		}

		public FilmmakerTrack(T defVal)
		{
			this.DefaultValue = defVal;
			this._unordered = new List<FilmmakerKeyframe<T>>();
		}

		public void AddOrReplace(T val, FilmmakerBlendPreset blend)
		{
			this.AddOrReplace(val, blend, FilmmakerTimelineManager.TimeFrames);
		}

		public void AddOrReplace(T val, FilmmakerBlendPreset blend, int timeFrames)
		{
			this.ClearFrame(timeFrames);
			FilmmakerKeyframe<T> filmmakerKeyframe = new FilmmakerKeyframe<T>
			{
				TimeFrames = timeFrames,
				Value = val,
				BlendCurve = blend
			};
			this._unordered.Add(filmmakerKeyframe);
			this._ordered = null;
		}

		public void ClearAll()
		{
			this._unordered.Clear();
			this._ordered = null;
		}

		public void ClearFrame()
		{
			this.ClearFrame(FilmmakerTimelineManager.TimeFrames);
		}

		public void ClearFrame(int frameTimes)
		{
			int count = this._unordered.Count;
			for (int i = 0; i < count; i++)
			{
				if (this._unordered[i].TimeFrames == frameTimes)
				{
					this._ordered = null;
					this._unordered.RemoveAt(i);
					return;
				}
			}
		}

		public bool TryGetNextFrame(int startTime, out int next)
		{
			int num = this.Keyframes.Length;
			for (int i = 0; i < num; i++)
			{
				next = this.Keyframes[i].TimeFrames;
				if (next > startTime)
				{
					return true;
				}
			}
			next = 0;
			return false;
		}

		public bool TryGetPrevFrame(int startTime, out int prev)
		{
			for (int i = this.Keyframes.Length - 1; i >= 0; i--)
			{
				prev = this.Keyframes[i].TimeFrames;
				if (prev < startTime)
				{
					return true;
				}
			}
			prev = 0;
			return false;
		}

		public readonly T DefaultValue;

		private readonly List<FilmmakerKeyframe<T>> _unordered;

		private FilmmakerKeyframe<T>[] _ordered;
	}
}
