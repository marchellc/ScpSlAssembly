using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NorthwoodLib.Pools;
using RelativePositioning;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioPooling;

public class AudioSourcePoolManager : MonoBehaviour
{
	[SerializeField]
	private CurvePreset[] _curves;

	[SerializeField]
	private ChannelPreset[] _channels;

	[SerializeField]
	private PooledAudioSource _template;

	private const float DefaultMaxDistance = 10f;

	private static bool _initialized;

	private static AudioSourcePoolManager _singleton;

	private static long _totalInstantiated;

	private static long _totalRecycled;

	private static long _totalRejected;

	private static long _totalDestroyed;

	private static readonly Queue<PooledAudioSource> FreeSources = new Queue<PooledAudioSource>();

	private static readonly Queue<PooledAudioSource> UpdateQueue = new Queue<PooledAudioSource>();

	private void Awake()
	{
		AudioSourcePoolManager._singleton = this;
		AudioSourcePoolManager._initialized = true;
	}

	private void OnDestroy()
	{
		AudioSourcePoolManager._initialized = false;
		AudioSourcePoolManager._totalDestroyed += AudioSourcePoolManager.FreeSources.Count + AudioSourcePoolManager.UpdateQueue.Count;
		AudioSourcePoolManager.FreeSources.Clear();
		AudioSourcePoolManager.UpdateQueue.Clear();
	}

	private void Update()
	{
		AudioSourcePoolManager.UpdateNextInstance();
	}

	private static void UpdateNextInstance()
	{
		if (AudioSourcePoolManager.UpdateQueue.TryDequeue(out var result))
		{
			if (result == null)
			{
				AudioSourcePoolManager._totalRejected++;
			}
			else if (result.AllowRecycling)
			{
				result.FastTransform.SetParent(null, worldPositionStays: false);
				AudioSourcePoolManager.FreeSources.Enqueue(result);
			}
			else
			{
				AudioSourcePoolManager.UpdateQueue.Enqueue(result);
			}
		}
	}

	public static PooledAudioSource CreateNewSource()
	{
		if (AudioSourcePoolManager._initialized)
		{
			return UnityEngine.Object.Instantiate(AudioSourcePoolManager._singleton._template);
		}
		Debug.LogWarning("Attempting to create a source without pool manager present.");
		return new GameObject("Fallback Audio Source", typeof(AudioSource)).AddComponent<PooledAudioSource>();
	}

	public static PooledAudioSource Play2D(AudioClip sound, float volume = 1f, MixerChannel channel = MixerChannel.DefaultSfx, float pitchScale = 1f)
	{
		PooledAudioSource free = AudioSourcePoolManager.GetFree(sound, FalloffType.Linear, channel, 0f);
		free.Source.volume = volume;
		free.Source.pitch = pitchScale;
		free.Source.Play();
		return free;
	}

	public static PooledAudioSource Play2DWithParent(AudioClip sound, Transform parent, float volume = 1f, MixerChannel channel = MixerChannel.DefaultSfx, float pitchScale = 1f)
	{
		PooledAudioSource free = AudioSourcePoolManager.GetFree(sound, FalloffType.Linear, channel, 0f);
		free.FastTransform.SetParent(parent, worldPositionStays: false);
		free.Source.volume = volume;
		free.Source.pitch = pitchScale;
		if (parent.gameObject.activeInHierarchy)
		{
			free.Source.Play();
		}
		return free;
	}

	public static PooledAudioSource PlayAtPosition(AudioClip sound, Vector3 position, float maxDistance = 10f, float volume = 1f, FalloffType falloffType = FalloffType.Exponential, MixerChannel channel = MixerChannel.DefaultSfx, float pitchScale = 1f)
	{
		PooledAudioSource free = AudioSourcePoolManager.GetFree(sound, falloffType, channel, 1f);
		free.FastTransform.position = position;
		AudioSource source = free.Source;
		source.maxDistance = maxDistance;
		source.volume = volume;
		source.pitch = pitchScale;
		source.Play();
		return free;
	}

	public static PooledAudioSource PlayAtPosition(AudioClip sound, RelativePosition relativePosition, float maxDistance = 10f, float volume = 1f, FalloffType falloffType = FalloffType.Exponential, MixerChannel channel = MixerChannel.DefaultSfx, float pitchScale = 1f)
	{
		if (!WaypointBase.TryGetWaypoint(relativePosition.WaypointId, out var wp))
		{
			return AudioSourcePoolManager.PlayAtPosition(sound, relativePosition.Relative, maxDistance, volume, falloffType, channel);
		}
		PooledAudioSource free = AudioSourcePoolManager.GetFree(sound, falloffType, channel, 1f);
		free.FastTransform.SetParent(wp.transform, worldPositionStays: false);
		free.FastTransform.position = wp.GetWorldspacePosition(relativePosition.Relative);
		AudioSource source = free.Source;
		source.maxDistance = maxDistance;
		source.volume = volume;
		source.pitch = pitchScale;
		if (wp.gameObject.activeInHierarchy)
		{
			source.Play();
		}
		return free;
	}

	public static PooledAudioSource PlayOnTransform(AudioClip sound, Transform trackedTransform, float maxDistance = 10f, float volume = 1f, FalloffType falloffType = FalloffType.Exponential, MixerChannel channel = MixerChannel.DefaultSfx, float pitchScale = 1f)
	{
		PooledAudioSource free = AudioSourcePoolManager.GetFree(sound, falloffType, channel, 1f);
		free.FastTransform.SetParent(trackedTransform, worldPositionStays: false);
		free.FastTransform.localPosition = Vector3.zero;
		AudioSource source = free.Source;
		source.maxDistance = maxDistance;
		source.volume = volume;
		source.pitch = pitchScale;
		if (trackedTransform.gameObject.activeInHierarchy)
		{
			source.Play();
		}
		return free;
	}

	public static PooledAudioSource GetFree(AudioClip sound, FalloffType falloffType, MixerChannel channel, float spatial)
	{
		PooledAudioSource free = AudioSourcePoolManager.GetFree();
		AudioSourcePoolManager.ApplyStandardSettings(free.Source, sound, falloffType, channel, spatial);
		if (!AudioSourcePoolManager._initialized)
		{
			free.Source.enabled = false;
		}
		return free;
	}

	public static PooledAudioSource GetFree()
	{
		PooledAudioSource result;
		while (AudioSourcePoolManager.FreeSources.TryDequeue(out result))
		{
			if (!(result == null))
			{
				AudioSourcePoolManager.UpdateQueue.Enqueue(result);
				result.OnRecycled();
				AudioSourcePoolManager._totalRecycled++;
				return result;
			}
		}
		AudioSourcePoolManager._totalInstantiated++;
		PooledAudioSource pooledAudioSource = AudioSourcePoolManager.CreateNewSource();
		AudioSourcePoolManager.UpdateQueue.Enqueue(pooledAudioSource);
		return pooledAudioSource;
	}

	public static void ApplyStandardSettings(AudioSource src, AudioClip sound, FalloffType falloffType, MixerChannel channel, float spatial, float maxDistance = 10f)
	{
		AnimationCurve falloffCurve = AudioSourcePoolManager.GetFalloffCurve(falloffType);
		AudioMixerGroup mixerGroup = AudioSourcePoolManager.GetMixerGroup(channel);
		if (falloffCurve != null)
		{
			src.rolloffMode = AudioRolloffMode.Custom;
			src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, falloffCurve);
		}
		else
		{
			src.rolloffMode = AudioRolloffMode.Linear;
		}
		src.enabled = true;
		src.playOnAwake = false;
		src.loop = false;
		src.dopplerLevel = 0f;
		src.panStereo = 0f;
		src.volume = 1f;
		src.spatialBlend = spatial;
		src.spread = 360f * (1f - spatial);
		src.minDistance = Mathf.Min(1f, maxDistance / 2f);
		src.maxDistance = maxDistance;
		src.outputAudioMixerGroup = mixerGroup;
		src.clip = sound;
		src.reverbZoneMix = 1f;
		src.pitch = 1f;
		src.mute = false;
	}

	public static AudioMixerGroup GetMixerGroup(MixerChannel channel)
	{
		if (!AudioSourcePoolManager._initialized)
		{
			return null;
		}
		ChannelPreset[] channels = AudioSourcePoolManager._singleton._channels;
		for (int i = 0; i < channels.Length; i++)
		{
			ChannelPreset channelPreset = channels[i];
			if (channelPreset.Type == channel)
			{
				return channelPreset.Group;
			}
		}
		throw new InvalidOperationException("Channel \"" + channel.ToString() + "\" is not defined in the AudioSourcePoolManager.");
	}

	public static AnimationCurve GetFalloffCurve(FalloffType falloffType)
	{
		if (!AudioSourcePoolManager._initialized)
		{
			return null;
		}
		CurvePreset[] curves = AudioSourcePoolManager._singleton._curves;
		foreach (CurvePreset curvePreset in curves)
		{
			if (curvePreset.Type == falloffType)
			{
				return curvePreset.FalloffCurve;
			}
		}
		throw new InvalidOperationException("Curve for falloff type \"" + falloffType.ToString() + "\" is not defined in the AudioSourcePoolManager.");
	}

	public static string ProcessConsoleCommand(bool forceUpdate)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		if (forceUpdate)
		{
			int count = AudioSourcePoolManager.UpdateQueue.Count;
			for (int i = 0; i < count; i++)
			{
				AudioSourcePoolManager.UpdateNextInstance();
			}
			stringBuilder.AppendLine("-- Returning free audio sources to the pool --");
		}
		stringBuilder.AppendLine("POOLABLE AUDIO SYSTEM DATA");
		stringBuilder.AppendLine("\nThis scene:");
		stringBuilder.AppendLine("  Active total: " + AudioSourcePoolManager.UpdateQueue.Count);
		stringBuilder.AppendLine("  Active and locked: " + AudioSourcePoolManager.UpdateQueue.Count((PooledAudioSource x) => x != null && x.Locked));
		stringBuilder.AppendLine("  Active and awaiting recycling: " + AudioSourcePoolManager.UpdateQueue.Count((PooledAudioSource x) => x != null && x.AllowRecycling));
		stringBuilder.AppendLine("  Active and awaiting removal: " + AudioSourcePoolManager.UpdateQueue.Count((PooledAudioSource x) => x == null));
		stringBuilder.AppendLine("  Available in pool: " + AudioSourcePoolManager.FreeSources.Count);
		stringBuilder.AppendLine("\nTotal game session turnover:");
		stringBuilder.AppendLine("  New instantiations: " + AudioSourcePoolManager._totalInstantiated);
		stringBuilder.AppendLine("  Recycled from pool: " + AudioSourcePoolManager._totalRecycled);
		stringBuilder.AppendLine("  Null rejections: " + AudioSourcePoolManager._totalRejected);
		stringBuilder.AppendLine("  Destroyed during scene swaps: " + AudioSourcePoolManager._totalDestroyed);
		if (AudioSourcePoolManager._initialized)
		{
			bool flag = AudioSourcePoolManager._singleton != null;
			stringBuilder.AppendLine("\nSingleton status: " + (flag ? "Active" : "Null"));
		}
		else
		{
			stringBuilder.AppendLine("\nPooling is not available for this scene");
		}
		return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}
}
