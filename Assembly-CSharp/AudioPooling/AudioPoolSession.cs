using System;
using UnityEngine;

namespace AudioPooling
{
	public readonly struct AudioPoolSession : IEquatable<AudioPoolSession>
	{
		public bool SameSession
		{
			get
			{
				return this.HandledInstance != null && !this.HandledInstance.Pooled && this.HandledInstance.TotalRecycles == this._sessionId;
			}
		}

		public bool IsPlaying
		{
			get
			{
				return this.SameSession && this.Source.isPlaying;
			}
		}

		public AudioSource Source
		{
			get
			{
				return this.HandledInstance.Source;
			}
		}

		public AudioPoolSession(PooledAudioSource subject)
		{
			this.HandledInstance = subject;
			this._sessionId = subject.TotalRecycles;
		}

		public bool Equals(AudioPoolSession other)
		{
			return this.HandledInstance == other.HandledInstance && this._sessionId == other._sessionId;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine<ulong, int>(this._sessionId, (this.HandledInstance == null) ? 0 : this.HandledInstance.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (obj is AudioPoolSession)
			{
				AudioPoolSession audioPoolSession = (AudioPoolSession)obj;
				return this.Equals(audioPoolSession);
			}
			return false;
		}

		public override string ToString()
		{
			return base.ToString();
		}

		public static bool operator ==(AudioPoolSession left, AudioPoolSession right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AudioPoolSession left, AudioPoolSession right)
		{
			return !left.Equals(right);
		}

		private readonly ulong _sessionId;

		public readonly PooledAudioSource HandledInstance;
	}
}
