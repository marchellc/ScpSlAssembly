using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using NorthwoodLib.Pools;
using Respawning.Objectives;
using UnityEngine;
using UserSettings;

namespace Respawning.Graphics
{
	public class ObjectiveFeedManager : MonoBehaviour
	{
		public static ObjectiveFeedManager Singleton { get; private set; }

		public bool IsFeedEnabled
		{
			get
			{
				return this._isFeedEnabled;
			}
			private set
			{
				this._isFeedEnabled = value;
				this._animator.SetBool("IsEnabled", value);
			}
		}

		private bool InUse
		{
			get
			{
				ObjectiveFeedEntry[] feedEntries = this._feedEntries;
				for (int i = 0; i < feedEntries.Length; i++)
				{
					if (feedEntries[i].isActiveAndEnabled)
					{
						return true;
					}
				}
				return false;
			}
		}

		public static void ClientObjectiveCompletionMessage(ObjectiveCompletionMessage message)
		{
			if (NetworkServer.active)
			{
				Debug.LogError("Unable to receive ObjectiveCompletionMessage on a server.");
				return;
			}
			if (ObjectiveFeedManager.Singleton == null || !ObjectiveFeedManager.Singleton.isActiveAndEnabled)
			{
				return;
			}
			IFootprintObjective footprintObjective = message.Objective as IFootprintObjective;
			if (footprintObjective == null)
			{
				return;
			}
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			footprintObjective.ObjectiveFootprint.ClientCompletionText(stringBuilder);
			ObjectiveFeedManager.ObjectiveEntryQueue.Enqueue(stringBuilder.ToString());
			StringBuilderPool.Shared.Return(stringBuilder);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<ObjectiveCompletionMessage>(new Action<ObjectiveCompletionMessage>(ObjectiveFeedManager.ClientObjectiveCompletionMessage), true);
			};
		}

		private void Awake()
		{
			ObjectiveFeedManager.Singleton = this;
			UserSetting<bool>.AddListener<RespawnSetting>(RespawnSetting.ObjectiveFeedVisible, new Action<bool>(this.SetEnabled));
			this.IsFeedEnabled = UserSetting<bool>.Get<RespawnSetting>(RespawnSetting.ObjectiveFeedVisible);
		}

		private void OnDestroy()
		{
			UserSetting<bool>.RemoveListener<RespawnSetting>(RespawnSetting.ObjectiveFeedVisible, new Action<bool>(this.SetEnabled));
		}

		private void Start()
		{
			ObjectiveFeedEntry[] feedEntries = this._feedEntries;
			for (int i = 0; i < feedEntries.Length; i++)
			{
				feedEntries[i].gameObject.SetActive(false);
			}
		}

		private void Update()
		{
			if (ObjectiveFeedManager.ObjectiveEntryQueue.Count == 0)
			{
				return;
			}
			foreach (ObjectiveFeedEntry objectiveFeedEntry in this._feedEntries)
			{
				if (!objectiveFeedEntry.isActiveAndEnabled)
				{
					string text;
					if (!ObjectiveFeedManager.ObjectiveEntryQueue.TryDequeue(out text))
					{
						return;
					}
					objectiveFeedEntry.CreateDisplay(text);
				}
			}
		}

		private void OnDisable()
		{
			ObjectiveFeedManager.ObjectiveEntryQueue.Clear();
		}

		private void SetEnabled(bool isEnabled)
		{
			this.IsFeedEnabled = isEnabled;
			if (isEnabled)
			{
				if (this.InUse)
				{
					return;
				}
				string text = Translations.Get<FootprintsTranslation>(FootprintsTranslation.ObjectiveFeedExample);
				ObjectiveFeedManager.ObjectiveEntryQueue.Enqueue(text);
			}
		}

		private const string FeedEnabledAnimKey = "IsEnabled";

		public static Queue<string> ObjectiveEntryQueue = new Queue<string>();

		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private ObjectiveFeedEntry[] _feedEntries;

		private bool _isFeedEnabled;
	}
}
