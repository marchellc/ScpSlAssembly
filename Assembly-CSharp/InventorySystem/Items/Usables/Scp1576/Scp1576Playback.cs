using System;
using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles;
using UnityEngine;
using Utils.NonAllocLINQ;
using VoiceChat;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Playback : SingleBufferPlayback, IGlobalPlayback, IPoolResettable, IPoolSpawnable
{
	private static readonly Queue<Scp1576Playback> Pool = new Queue<Scp1576Playback>();

	private static readonly Dictionary<Scp1576Source, Dictionary<ReferenceHub, Scp1576Playback>> ActiveInstances = new Dictionary<Scp1576Source, Dictionary<ReferenceHub, Scp1576Playback>>();

	private static bool _anyCreated;

	private static bool _templateCacheSet;

	private static Scp1576Playback _templateCache;

	[SerializeField]
	private bool _playerMode;

	private Transform _tr;

	private Scp1576Source _source;

	private bool _spawned;

	public static Scp1576Playback Template
	{
		get
		{
			if (Scp1576Playback._templateCacheSet)
			{
				return Scp1576Playback._templateCache;
			}
			if (!InventoryItemLoader.TryGetItem<Scp1576Item>(ItemType.SCP1576, out var result))
			{
				throw new InvalidOperationException("SCP-1576 template not found!");
			}
			Scp1576Playback._templateCache = result.PlaybackTemplate;
			Scp1576Playback._templateCacheSet = true;
			return Scp1576Playback._templateCache;
		}
	}

	public ReferenceHub Owner { get; set; }

	public virtual bool GlobalChatActive
	{
		get
		{
			if (!this._spawned || !this._source.HideGlobalIndicator)
			{
				return this.MaxSamples > 0;
			}
			return false;
		}
	}

	public virtual Color GlobalChatColor => this.Owner.serverRoles.GetVoiceColor();

	public virtual string GlobalChatName => this.Owner.nicknameSync.DisplayName;

	public virtual float GlobalChatLoudness => base.Loudness;

	public GlobalChatIconType GlobalChatIcon => GlobalChatIconType.Avatar;

	public void SpawnObject()
	{
		if (this._playerMode && base.transform.parent.TryGetComponent<HumanRole>(out var component) && component.TryGetOwner(out var hub))
		{
			this.Owner = hub;
			GlobalChatIndicatorManager.Subscribe(this, this.Owner);
		}
	}

	public void ResetObject()
	{
		if (this._playerMode)
		{
			GlobalChatIndicatorManager.Unsubscribe(this);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._tr = base.transform;
	}

	private void OnDestroy()
	{
		if (Scp1576Playback._anyCreated)
		{
			Scp1576Playback.Pool.Clear();
			Scp1576Playback.ActiveInstances.Clear();
			Scp1576Playback._anyCreated = false;
		}
	}

	private void LateUpdate()
	{
		if (this._spawned)
		{
			this._tr.position = this._source.Position;
		}
	}

	public static void DistributeSamples(ReferenceHub speaker, float[] samples, int len)
	{
		foreach (Scp1576Source instance in Scp1576Source.Instances)
		{
			Scp1576Playback.GetOrAdd(speaker, instance).Buffer.Write(samples, len);
		}
	}

	private static Scp1576Playback GetOrAdd(ReferenceHub player, Scp1576Source source)
	{
		if (Scp1576Playback.ActiveInstances.TryGetValue(source, out var value) && value.TryGetValue(player, out var value2))
		{
			return value2;
		}
		if (!Scp1576Playback.Pool.TryDequeue(out value2))
		{
			value2 = UnityEngine.Object.Instantiate(Scp1576Playback.Template);
			Scp1576Playback._anyCreated = true;
		}
		value2._spawned = true;
		value2.Owner = player;
		value2._source = source;
		Scp1576Playback.ActiveInstances.GetOrAdd(source, () => new Dictionary<ReferenceHub, Scp1576Playback>())[player] = value2;
		GlobalChatIndicatorManager.Subscribe(value2, player);
		return value2;
	}

	private static void ReturnToPool(Scp1576Playback playback)
	{
		if (Scp1576Playback.ActiveInstances.TryGetValue(playback._source, out var value))
		{
			value.Remove(playback.Owner);
		}
		Scp1576Playback.Pool.Enqueue(playback);
		playback._spawned = false;
		GlobalChatIndicatorManager.Unsubscribe(playback);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			Scp1576Playback.ActiveInstances.ForEachValue(delegate(Dictionary<ReferenceHub, Scp1576Playback> x)
			{
				if (x.TryGetValue(hub, out var value))
				{
					x.Remove(hub);
					Scp1576Playback.ReturnToPool(value);
				}
			});
		};
		Scp1576Source.OnRemoved = (Action<Scp1576Source>)Delegate.Combine(Scp1576Source.OnRemoved, (Action<Scp1576Source>)delegate(Scp1576Source src)
		{
			if (Scp1576Playback.ActiveInstances.TryGetValue(src, out var value))
			{
				Scp1576Playback.ActiveInstances.Remove(src);
				value.ForEachValue(ReturnToPool);
			}
		});
	}
}
