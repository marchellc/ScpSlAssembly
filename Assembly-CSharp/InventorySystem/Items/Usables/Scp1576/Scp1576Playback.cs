using System;
using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles;
using UnityEngine;
using Utils.NonAllocLINQ;
using VoiceChat;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Usables.Scp1576
{
	public class Scp1576Playback : SingleBufferPlayback, IGlobalPlayback, IPoolResettable, IPoolSpawnable
	{
		public static Scp1576Playback Template
		{
			get
			{
				if (Scp1576Playback._templateCacheSet)
				{
					return Scp1576Playback._templateCache;
				}
				Scp1576Item scp1576Item;
				if (!InventoryItemLoader.TryGetItem<Scp1576Item>(ItemType.SCP1576, out scp1576Item))
				{
					throw new InvalidOperationException("SCP-1576 template not found!");
				}
				Scp1576Playback._templateCache = scp1576Item.PlaybackTemplate;
				Scp1576Playback._templateCacheSet = true;
				return Scp1576Playback._templateCache;
			}
		}

		public ReferenceHub Owner { get; set; }

		public virtual bool GlobalChatActive
		{
			get
			{
				return (!this._spawned || !this._source.HideGlobalIndicator) && this.MaxSamples > 0;
			}
		}

		public virtual Color GlobalChatColor
		{
			get
			{
				return this.Owner.serverRoles.GetVoiceColor();
			}
		}

		public virtual string GlobalChatName
		{
			get
			{
				return this.Owner.nicknameSync.DisplayName;
			}
		}

		public virtual float GlobalChatLoudness
		{
			get
			{
				return base.Loudness;
			}
		}

		public GlobalChatIconType GlobalChatIcon
		{
			get
			{
				return GlobalChatIconType.Avatar;
			}
		}

		public void SpawnObject()
		{
			if (!this._playerMode)
			{
				return;
			}
			HumanRole humanRole;
			if (!base.transform.parent.TryGetComponent<HumanRole>(out humanRole))
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!humanRole.TryGetOwner(out referenceHub))
			{
				return;
			}
			this.Owner = referenceHub;
			GlobalChatIndicatorManager.Subscribe(this, this.Owner);
		}

		public void ResetObject()
		{
			if (!this._playerMode)
			{
				return;
			}
			GlobalChatIndicatorManager.Unsubscribe(this);
		}

		protected override void Awake()
		{
			base.Awake();
			this._tr = base.transform;
		}

		private void OnDestroy()
		{
			if (!Scp1576Playback._anyCreated)
			{
				return;
			}
			Scp1576Playback.Pool.Clear();
			Scp1576Playback.ActiveInstances.Clear();
			Scp1576Playback._anyCreated = false;
		}

		private void LateUpdate()
		{
			if (!this._spawned)
			{
				return;
			}
			this._tr.position = this._source.Position;
		}

		public static void DistributeSamples(ReferenceHub speaker, float[] samples, int len)
		{
			foreach (Scp1576Source scp1576Source in Scp1576Source.Instances)
			{
				Scp1576Playback.GetOrAdd(speaker, scp1576Source).Buffer.Write(samples, len);
			}
		}

		private static Scp1576Playback GetOrAdd(ReferenceHub player, Scp1576Source source)
		{
			Dictionary<ReferenceHub, Scp1576Playback> dictionary;
			Scp1576Playback scp1576Playback;
			if (Scp1576Playback.ActiveInstances.TryGetValue(source, out dictionary) && dictionary.TryGetValue(player, out scp1576Playback))
			{
				return scp1576Playback;
			}
			if (!Scp1576Playback.Pool.TryDequeue(out scp1576Playback))
			{
				scp1576Playback = global::UnityEngine.Object.Instantiate<Scp1576Playback>(Scp1576Playback.Template);
				Scp1576Playback._anyCreated = true;
			}
			scp1576Playback._spawned = true;
			scp1576Playback.Owner = player;
			scp1576Playback._source = source;
			Scp1576Playback.ActiveInstances.GetOrAdd(source, () => new Dictionary<ReferenceHub, Scp1576Playback>())[player] = scp1576Playback;
			GlobalChatIndicatorManager.Subscribe(scp1576Playback, player);
			return scp1576Playback;
		}

		private static void ReturnToPool(Scp1576Playback playback)
		{
			Dictionary<ReferenceHub, Scp1576Playback> dictionary;
			if (Scp1576Playback.ActiveInstances.TryGetValue(playback._source, out dictionary))
			{
				dictionary.Remove(playback.Owner);
			}
			Scp1576Playback.Pool.Enqueue(playback);
			playback._spawned = false;
			GlobalChatIndicatorManager.Unsubscribe(playback);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				Scp1576Playback.ActiveInstances.ForEachValue(delegate(Dictionary<ReferenceHub, Scp1576Playback> x)
				{
					Scp1576Playback scp1576Playback;
					if (!x.TryGetValue(hub, out scp1576Playback))
					{
						return;
					}
					x.Remove(hub);
					Scp1576Playback.ReturnToPool(scp1576Playback);
				});
			}));
			Scp1576Source.OnRemoved = (Action<Scp1576Source>)Delegate.Combine(Scp1576Source.OnRemoved, new Action<Scp1576Source>(delegate(Scp1576Source src)
			{
				Dictionary<ReferenceHub, Scp1576Playback> dictionary;
				if (!Scp1576Playback.ActiveInstances.TryGetValue(src, out dictionary))
				{
					return;
				}
				Scp1576Playback.ActiveInstances.Remove(src);
				dictionary.ForEachValue(new Action<Scp1576Playback>(Scp1576Playback.ReturnToPool));
			}));
		}

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
	}
}
