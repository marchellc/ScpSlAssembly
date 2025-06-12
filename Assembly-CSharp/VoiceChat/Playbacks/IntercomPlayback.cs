using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat.Playbacks;

public class IntercomPlayback : SingleBufferPlayback, IGlobalPlayback
{
	private bool _isTemplate;

	private ReferenceHub _lastSpeaker;

	private static bool _templateSet;

	private static IntercomPlayback _template;

	private static int _instancesCnt;

	private static readonly List<IntercomPlayback> Instances = new List<IntercomPlayback>();

	public bool GlobalChatActive => this.MaxSamples > 0;

	public Color GlobalChatColor { get; private set; }

	public string GlobalChatName { get; private set; }

	public float GlobalChatLoudness => base.Loudness;

	public GlobalChatIconType GlobalChatIcon => GlobalChatIconType.Intercom;

	protected override void Awake()
	{
		base.Awake();
		IntercomPlayback._instancesCnt++;
		IntercomPlayback.Instances.Add(this);
		GlobalChatIndicatorManager.Subscribe(this, null);
		if (!IntercomPlayback._templateSet)
		{
			IntercomPlayback._template = this;
			this._isTemplate = true;
			IntercomPlayback._templateSet = true;
		}
	}

	private void OnDestroy()
	{
		GlobalChatIndicatorManager.Unsubscribe(this);
		if (this._isTemplate)
		{
			IntercomPlayback._templateSet = false;
			IntercomPlayback._instancesCnt = 0;
			IntercomPlayback.Instances.Clear();
		}
	}

	private void SetSpeaker(ReferenceHub speaker)
	{
		this._lastSpeaker = speaker;
		this.GlobalChatName = speaker.nicknameSync.DisplayName;
		this.GlobalChatColor = speaker.serverRoles.GetVoiceColor();
	}

	public static void ProcessSamples(ReferenceHub ply, float[] samples, int len)
	{
		if (!IntercomPlayback._templateSet)
		{
			return;
		}
		bool flag = false;
		IntercomPlayback intercomPlayback = null;
		for (int i = 0; i < IntercomPlayback._instancesCnt; i++)
		{
			IntercomPlayback intercomPlayback2 = IntercomPlayback.Instances[i];
			if (intercomPlayback2._lastSpeaker == ply)
			{
				intercomPlayback2.Buffer.Write(samples, len);
				return;
			}
			if (!flag && intercomPlayback2.MaxSamples == 0)
			{
				intercomPlayback = intercomPlayback2;
				flag = true;
			}
		}
		if (!flag)
		{
			intercomPlayback = Object.Instantiate(IntercomPlayback._template);
			intercomPlayback.Buffer.Clear();
		}
		intercomPlayback.SetSpeaker(ply);
		intercomPlayback.Buffer.Write(samples, len);
	}
}
