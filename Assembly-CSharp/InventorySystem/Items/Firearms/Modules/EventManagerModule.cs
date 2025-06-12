using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class EventManagerModule : ModuleBase
{
	private class AnimatorEventProcessor
	{
		private readonly int[] _layers;

		private readonly Animator _animator;

		private readonly LayerEventProcessor[] _currentLayers;

		private readonly LayerEventProcessor[] _nextLayers;

		private readonly Dictionary<int, float>[] _continuationTimes;

		private readonly EventManagerModule _eventManager;

		public AnimatorEventProcessor(EventManagerModule eventManager, Animator animator)
		{
			this._animator = animator;
			this._eventManager = eventManager;
			this._layers = eventManager.AffectedLayers.Layers;
			int num = this._layers.Length;
			this._currentLayers = new LayerEventProcessor[num];
			this._nextLayers = new LayerEventProcessor[num];
			this._continuationTimes = new Dictionary<int, float>[num];
			for (int i = 0; i < num; i++)
			{
				int layerIndex = this._layers[i];
				this._currentLayers[i] = new LayerEventProcessor(this._eventManager, isCurrent: true, layerIndex, animator);
				this._nextLayers[i] = new LayerEventProcessor(this._eventManager, isCurrent: false, layerIndex, animator);
				this._continuationTimes[i] = new Dictionary<int, float>();
			}
		}

		public void ProcessAll()
		{
			for (int i = 0; i < this._layers.Length; i++)
			{
				int layerIndex = this._layers[i];
				Dictionary<int, float> continuationTimes = this._continuationTimes[i];
				this._currentLayers[i].Process(this._animator.GetCurrentAnimatorStateInfo(layerIndex), continuationTimes);
				this._nextLayers[i].Process(this._animator.GetNextAnimatorStateInfo(layerIndex), continuationTimes);
			}
		}
	}

	private class LayerEventProcessor
	{
		private readonly EventManagerModule _eventManager;

		private readonly bool _isCurrent;

		private readonly Animator _anim;

		private readonly int _layerIndex;

		private List<int> _prevEvents;

		private int _curHash;

		private float _lastFrame;

		private bool _hasEvents;

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
			foreach (int prevEvent in this._prevEvents)
			{
				FirearmEvent firearmEvent = this._eventManager.Events[prevEvent];
				float num = stateInfo.normalizedTime;
				if (stateInfo.loop)
				{
					num -= (float)(int)num;
				}
				this._lastFrame = num * firearmEvent.LengthFrames;
				if (lastFrame < firearmEvent.Frame && this._lastFrame >= firearmEvent.Frame)
				{
					EventInvocationDetails data = new EventInvocationDetails(stateInfo, this._anim, this._layerIndex);
					firearmEvent.InvokeSafe(data);
				}
				if (!this._isCurrent)
				{
					continuationTimes[this._curHash] = this._lastFrame;
				}
			}
		}
	}

	private static readonly Dictionary<ItemType, Dictionary<int, List<int>>> FirearmsToNameHashesToIndexes = new Dictionary<ItemType, Dictionary<int, List<int>>>();

	public List<FirearmEvent> Events = new List<FirearmEvent>();

	public AnimatorLayerMask AffectedLayers;

	private Dictionary<int, List<int>> _nameHashesToIndexes;

	private AnimatorEventProcessor _processor;

	public bool SkippingForward { get; private set; }

	public event Action<int> OnEventRelayed;

	public void AddEventsForClip(int hash, List<FirearmEvent> list)
	{
		if (!this._nameHashesToIndexes.TryGetValue(hash, out var value))
		{
			return;
		}
		foreach (int item in value)
		{
			list.Add(this.Events[item]);
		}
	}

	[ExposedFirearmEvent]
	public void RelayEvent(int guid)
	{
		this.OnEventRelayed?.Invoke(guid);
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		this.CacheIndexes();
		if (base.IsLocalPlayer)
		{
			this._processor = new AnimatorEventProcessor(this, SharedHandsController.Singleton.Hands);
		}
		else if (base.IsServer)
		{
			this._processor = new AnimatorEventProcessor(this, base.Firearm.ServerSideAnimator);
		}
	}

	internal override void SpectatorInit()
	{
		base.SpectatorInit();
		this.SkippingForward = true;
		this._processor = new AnimatorEventProcessor(this, SharedHandsController.Singleton.Hands);
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

	private void LateUpdate()
	{
		if (base.Firearm.IsEquipped)
		{
			this._processor?.ProcessAll();
			this.SkippingForward = false;
		}
	}

	[ContextMenu("Clear event cache")]
	private void ClearCache()
	{
		EventManagerModule.FirearmsToNameHashesToIndexes.Clear();
		this.CacheIndexes();
	}
}
