using System.Collections.Generic;
using System.Text;
using Mirror;
using NorthwoodLib.Pools;
using Respawning.Objectives;
using UnityEngine;
using UserSettings;

namespace Respawning.Graphics;

public class ObjectiveFeedManager : MonoBehaviour
{
	private const string FeedEnabledAnimKey = "IsEnabled";

	public static Queue<string> ObjectiveEntryQueue = new Queue<string>();

	[SerializeField]
	private Animator _animator;

	[SerializeField]
	private ObjectiveFeedEntry[] _feedEntries;

	private bool _isFeedEnabled;

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
		}
		else if (!(ObjectiveFeedManager.Singleton == null) && ObjectiveFeedManager.Singleton.isActiveAndEnabled && message.Objective is IFootprintObjective footprintObjective)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			footprintObjective.ObjectiveFootprint.ClientCompletionText(stringBuilder);
			ObjectiveFeedManager.ObjectiveEntryQueue.Enqueue(stringBuilder.ToString());
			StringBuilderPool.Shared.Return(stringBuilder);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<ObjectiveCompletionMessage>(ClientObjectiveCompletionMessage);
		};
	}

	private void Awake()
	{
		ObjectiveFeedManager.Singleton = this;
		UserSetting<bool>.AddListener(RespawnSetting.ObjectiveFeedVisible, SetEnabled);
		this.IsFeedEnabled = UserSetting<bool>.Get(RespawnSetting.ObjectiveFeedVisible);
	}

	private void OnDestroy()
	{
		UserSetting<bool>.RemoveListener(RespawnSetting.ObjectiveFeedVisible, SetEnabled);
	}

	private void Start()
	{
		ObjectiveFeedEntry[] feedEntries = this._feedEntries;
		for (int i = 0; i < feedEntries.Length; i++)
		{
			feedEntries[i].gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (ObjectiveFeedManager.ObjectiveEntryQueue.Count == 0)
		{
			return;
		}
		ObjectiveFeedEntry[] feedEntries = this._feedEntries;
		foreach (ObjectiveFeedEntry objectiveFeedEntry in feedEntries)
		{
			if (!objectiveFeedEntry.isActiveAndEnabled)
			{
				if (!ObjectiveFeedManager.ObjectiveEntryQueue.TryDequeue(out var result))
				{
					break;
				}
				objectiveFeedEntry.CreateDisplay(result);
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
		if (isEnabled && !this.InUse)
		{
			string item = Translations.Get(FootprintsTranslation.ObjectiveFeedExample);
			ObjectiveFeedManager.ObjectiveEntryQueue.Enqueue(item);
		}
	}
}
