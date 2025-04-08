using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class EventManagerModule : ModuleBase
	{
		public event Action<int> OnEventRelayed;

		public bool SkippingForward { get; private set; }

		public void AddEventsForClip(int hash, List<FirearmEvent> list)
		{
			List<int> list2;
			if (!this._nameHashesToIndexes.TryGetValue(hash, out list2))
			{
				return;
			}
			foreach (int num in list2)
			{
				list.Add(this.Events[num]);
			}
		}

		[ExposedFirearmEvent]
		public void RelayEvent(int guid)
		{
			Action<int> onEventRelayed = this.OnEventRelayed;
			if (onEventRelayed == null)
			{
				return;
			}
			onEventRelayed(guid);
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			this.CacheIndexes();
			if (base.IsLocalPlayer)
			{
				this.InitViewmodel();
				return;
			}
			if (base.IsServer)
			{
				this.InitServer();
			}
		}

		internal override void SpectatorInit()
		{
			base.SpectatorInit();
			this.SkippingForward = true;
			this.InitViewmodel();
			this.CacheIndexes();
		}

		private void CacheIndexes()
		{
			if (EventManagerModule.FirearmsToNameHashesToIndexes.TryGetValue(base.Firearm.ItemTypeId, out this._nameHashesToIndexes))
			{
				return;
			}
			this._nameHashesToIndexes = new Dictionary<int, List<int>>();
			EventManagerModule.FirearmsToNameHashesToIndexes[base.Firearm.ItemTypeId] = this._nameHashesToIndexes;
			for (int i = 0; i < this.Events.Count; i++)
			{
				this._nameHashesToIndexes.GetOrAdd(this.Events[i].NameHash, () => new List<int>()).Add(i);
			}
		}

		private void InitServer()
		{
			this._serverProcessor = new EventManagerModule.AnimatorEventProcessor(this, base.Firearm.ServerSideAnimator);
			base.Firearm.OnServerAnimatorMove += this._serverProcessor.ProcessAll;
		}

		private void InitViewmodel()
		{
			this._viewmodelProcessor = new EventManagerModule.AnimatorEventProcessor(this, SharedHandsController.Singleton.Hands);
		}

		private void LateUpdate()
		{
			if (!base.Firearm.IsEquipped)
			{
				return;
			}
			EventManagerModule.AnimatorEventProcessor viewmodelProcessor = this._viewmodelProcessor;
			if (viewmodelProcessor != null)
			{
				viewmodelProcessor.ProcessAll();
			}
			this.SkippingForward = false;
		}

		[ContextMenu("Clear event cache")]
		private void ClearCache()
		{
			EventManagerModule.FirearmsToNameHashesToIndexes.Clear();
			this.CacheIndexes();
		}

		private static readonly Dictionary<ItemType, Dictionary<int, List<int>>> FirearmsToNameHashesToIndexes = new Dictionary<ItemType, Dictionary<int, List<int>>>();

		public List<FirearmEvent> Events = new List<FirearmEvent>();

		public AnimatorLayerMask AffectedLayers;

		private Dictionary<int, List<int>> _nameHashesToIndexes;

		private EventManagerModule.AnimatorEventProcessor _serverProcessor;

		private EventManagerModule.AnimatorEventProcessor _viewmodelProcessor;

		private class AnimatorEventProcessor
		{
			public AnimatorEventProcessor(EventManagerModule eventManager, Animator animator)
			{
				this._animator = animator;
				this._eventManager = eventManager;
				this._layers = eventManager.AffectedLayers.Layers;
				int num = this._layers.Length;
				this._currentLayers = new EventManagerModule.LayerEventProcessor[num];
				this._nextLayers = new EventManagerModule.LayerEventProcessor[num];
				this._continuationTimes = new Dictionary<int, float>[num];
				for (int i = 0; i < num; i++)
				{
					int num2 = this._layers[i];
					this._currentLayers[i] = new EventManagerModule.LayerEventProcessor(this._eventManager, true, num2, animator);
					this._nextLayers[i] = new EventManagerModule.LayerEventProcessor(this._eventManager, false, num2, animator);
					this._continuationTimes[i] = new Dictionary<int, float>();
				}
			}

			public void ProcessAll()
			{
				for (int i = 0; i < this._layers.Length; i++)
				{
					int num = this._layers[i];
					Dictionary<int, float> dictionary = this._continuationTimes[i];
					this._currentLayers[i].Process(this._animator.GetCurrentAnimatorStateInfo(num), dictionary);
					this._nextLayers[i].Process(this._animator.GetNextAnimatorStateInfo(num), dictionary);
				}
			}

			private readonly int[] _layers;

			private readonly Animator _animator;

			private readonly EventManagerModule.LayerEventProcessor[] _currentLayers;

			private readonly EventManagerModule.LayerEventProcessor[] _nextLayers;

			private readonly Dictionary<int, float>[] _continuationTimes;

			private readonly EventManagerModule _eventManager;
		}

		private class LayerEventProcessor
		{
			public LayerEventProcessor(EventManagerModule eventManager, bool isCurrent, int layerIndex, Animator anim)
			{
				this._eventManager = eventManager;
				this._isCurrent = isCurrent;
				this._layerIndex = layerIndex;
				this._anim = anim;
			}

			public void Process(AnimatorStateInfo stateInfo, Dictionary<int, float> continuationTimes)
			{
				if (this._curHash != stateInfo.shortNameHash)
				{
					this._curHash = stateInfo.shortNameHash;
					this._lastFrame = (this._isCurrent ? continuationTimes.GetValueOrDefault(this._curHash) : 0f);
					this._hasEvents = this._eventManager._nameHashesToIndexes.TryGetValue(this._curHash, out this._prevEvents);
					continuationTimes[this._curHash] = 0f;
				}
				if (!this._hasEvents || this._curHash == 0)
				{
					return;
				}
				float lastFrame = this._lastFrame;
				foreach (int num in this._prevEvents)
				{
					FirearmEvent firearmEvent = this._eventManager.Events[num];
					float num2 = stateInfo.normalizedTime;
					if (stateInfo.loop)
					{
						num2 -= (float)((int)num2);
					}
					this._lastFrame = num2 * firearmEvent.LengthFrames;
					if (lastFrame < firearmEvent.Frame && this._lastFrame >= firearmEvent.Frame)
					{
						EventInvocationDetails eventInvocationDetails = new EventInvocationDetails(stateInfo, this._anim, this._layerIndex);
						firearmEvent.InvokeSafe(eventInvocationDetails);
					}
					if (!this._isCurrent)
					{
						continuationTimes[this._curHash] = this._lastFrame;
					}
				}
			}

			private readonly EventManagerModule _eventManager;

			private readonly bool _isCurrent;

			private readonly Animator _anim;

			private readonly int _layerIndex;

			private List<int> _prevEvents;

			private int _curHash;

			private float _lastFrame;

			private bool _hasEvents;
		}
	}
}
