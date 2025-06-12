using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkManagerUtils.Dummies;

public static class DummyActionCollector
{
	private class CachedActions
	{
		private readonly List<DummyAction> _actions;

		private readonly IRootDummyActionProvider[] _providers;

		private bool _everUpdated;

		public List<DummyAction> Actions
		{
			get
			{
				if (this.AnyDirty)
				{
					this.UpdateCache();
				}
				return this._actions;
			}
		}

		public bool AnyDirty
		{
			get
			{
				if (!this._everUpdated)
				{
					return true;
				}
				IRootDummyActionProvider[] providers = this._providers;
				for (int i = 0; i < providers.Length; i++)
				{
					if (providers[i].DummyActionsDirty)
					{
						return true;
					}
				}
				return false;
			}
		}

		public CachedActions(ReferenceHub hub)
		{
			this._actions = new List<DummyAction>();
			this._providers = hub.GetComponents<IRootDummyActionProvider>();
			this._everUpdated = false;
		}

		private void UpdateCache()
		{
			this._actions.Clear();
			IRootDummyActionProvider[] providers = this._providers;
			for (int i = 0; i < providers.Length; i++)
			{
				providers[i].PopulateDummyActions(this._actions.Add, AddCategory);
			}
			this._everUpdated = true;
		}

		private void AddCategory(string categoryName)
		{
			this._actions.Add(new DummyAction(categoryName, null));
		}
	}

	private static readonly Dictionary<ReferenceHub, CachedActions> CollectionCache = new Dictionary<ReferenceHub, CachedActions>();

	public static bool IsDirty(ReferenceHub hub)
	{
		return DummyActionCollector.GetCache(hub).AnyDirty;
	}

	public static List<DummyAction> ServerGetActions(ReferenceHub hub)
	{
		return DummyActionCollector.GetCache(hub).Actions;
	}

	private static CachedActions GetCache(ReferenceHub hub)
	{
		if (hub == null || !hub.IsDummy)
		{
			throw new ArgumentException("Provided argument is not a dummy.", "hub");
		}
		if (!DummyActionCollector.CollectionCache.TryGetValue(hub, out var value))
		{
			value = new CachedActions(hub);
			DummyActionCollector.CollectionCache[hub] = value;
		}
		return value;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ReferenceHub.OnPlayerRemoved += OnHubRemoved;
	}

	private static void OnHubRemoved(ReferenceHub hub)
	{
		if (hub.IsDummy)
		{
			DummyActionCollector.CollectionCache.Remove(hub);
		}
	}
}
