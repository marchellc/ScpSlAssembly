using System;
using UnityEngine;

namespace AudioPooling;

public readonly struct AudioPoolSession : IEquatable<AudioPoolSession>
{
	private readonly ulong _sessionId;

	public readonly PooledAudioSource HandledInstance;

	public bool SameSession
	{
		get
		{
			if (this.HandledInstance != null && !this.HandledInstance.Pooled)
			{
				return this.HandledInstance.TotalRecycles == this._sessionId;
			}
			return false;
		}
	}

	public bool IsPlaying
	{
		get
		{
			if (this.SameSession)
			{
				return this.Source.isPlaying;
			}
			return false;
		}
	}

	public AudioSource Source => this.HandledInstance.Source;

	public AudioPoolSession(PooledAudioSource subject)
	{
		this.HandledInstance = subject;
		this._sessionId = subject.TotalRecycles;
	}

	public bool Equals(AudioPoolSession other)
	{
		if (this.HandledInstance == other.HandledInstance)
		{
			return this._sessionId == other._sessionId;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(this._sessionId, (!(this.HandledInstance == null)) ? this.HandledInstance.GetHashCode() : 0);
	}

	public override bool Equals(object obj)
	{
		if (obj is AudioPoolSession other)
		{
			return this.Equals(other);
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
}
