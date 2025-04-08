using System;
using System.Collections.Generic;
using UnityEngine;
using VoiceChat.Playbacks;

namespace VoiceChat
{
	public class GlobalChatIndicatorManager : MonoBehaviour
	{
		private void Awake()
		{
			GlobalChatIndicatorManager._singleton = this;
			GlobalChatIndicatorManager._singletonSet = true;
		}

		private void OnDestroy()
		{
			GlobalChatIndicatorManager._singletonSet = false;
		}

		private void LateUpdate()
		{
			foreach (KeyValuePair<IGlobalPlayback, GlobalChatIndicator> keyValuePair in this._instances)
			{
				keyValuePair.Value.Refresh();
			}
		}

		private void SpawnIndicator(IGlobalPlayback igp, ReferenceHub ply)
		{
			GlobalChatIndicator globalChatIndicator;
			if (!this._pool.TryDequeue(out globalChatIndicator))
			{
				globalChatIndicator = global::UnityEngine.Object.Instantiate<GlobalChatIndicator>(this._template);
			}
			Transform transform = globalChatIndicator.transform;
			transform.SetParent(this._root);
			transform.localScale = Vector3.one;
			transform.SetAsLastSibling();
			globalChatIndicator.Setup(igp, ply);
			this._instances[igp] = globalChatIndicator;
		}

		private void ReturnIndicator(IGlobalPlayback igp)
		{
			GlobalChatIndicator globalChatIndicator;
			if (!this._instances.TryGetValue(igp, out globalChatIndicator))
			{
				return;
			}
			globalChatIndicator.gameObject.SetActive(false);
			this._pool.Enqueue(globalChatIndicator);
			this._instances.Remove(igp);
		}

		public static void Subscribe(IGlobalPlayback igp, ReferenceHub player)
		{
			GlobalChatIndicatorManager._singleton.SpawnIndicator(igp, player);
		}

		public static void Unsubscribe(IGlobalPlayback igp)
		{
			if (!GlobalChatIndicatorManager._singletonSet)
			{
				return;
			}
			GlobalChatIndicatorManager._singleton.ReturnIndicator(igp);
		}

		[SerializeField]
		private GlobalChatIndicator _template;

		[SerializeField]
		private RectTransform _root;

		private readonly Queue<GlobalChatIndicator> _pool = new Queue<GlobalChatIndicator>();

		private readonly Dictionary<IGlobalPlayback, GlobalChatIndicator> _instances = new Dictionary<IGlobalPlayback, GlobalChatIndicator>();

		private static GlobalChatIndicatorManager _singleton;

		private static bool _singletonSet;
	}
}
