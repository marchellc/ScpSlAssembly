using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MapGeneration;
using MapGeneration.Distributors;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079GeneratorNotification : Scp079SimpleNotification
{
	public static readonly HashSet<Scp079Generator> TrackedGens = new HashSet<Scp079Generator>();

	private readonly Scp079Generator _generator;

	private readonly StringBuilder _emptyBuilder;

	private readonly Stopwatch _activeStopwatch;

	private const string UnknownRoom = "UNKNOWN";

	private const string Format = "<color=red>0m 00s - {0}</color>";

	private const int MinuteDigit = 11;

	private const int TensDigit = 14;

	private const int SecsDigit = 15;

	private const int CharOffset = 48;

	private const int BlinkRate = 9;

	private const float BlinkDuration = 2.5f;

	private float _opacity;

	private bool IsActivating
	{
		get
		{
			if (this._generator != null)
			{
				return this._generator.Activating;
			}
			return false;
		}
	}

	public override float Opacity
	{
		get
		{
			float num = (this.IsActivating ? 1f : (-1f));
			return this._opacity = Mathf.Min(1f, this._opacity + num * Time.deltaTime / 0.18f);
		}
	}

	public override bool Delete
	{
		get
		{
			if (base.Delete)
			{
				Scp079GeneratorNotification.TrackedGens.Remove(this._generator);
				return true;
			}
			return false;
		}
	}

	protected override StringBuilder WrittenText
	{
		get
		{
			float num = (float)this._activeStopwatch.Elapsed.TotalSeconds;
			if (num < 2.5f && Mathf.RoundToInt(num * 9f) % 2 != 0)
			{
				return this._emptyBuilder;
			}
			StringBuilder writtenText = base.WrittenText;
			if (!this.IsActivating)
			{
				return writtenText;
			}
			int remainingTime = this._generator.RemainingTime;
			Scp079GeneratorNotification.OverrideStringBuilder(writtenText, 11, remainingTime / 60 + 48);
			Scp079GeneratorNotification.OverrideStringBuilder(writtenText, 14, remainingTime % 60 / 10 + 48);
			Scp079GeneratorNotification.OverrideStringBuilder(writtenText, 15, remainingTime % 60 % 10 + 48);
			return writtenText;
		}
	}

	public Scp079GeneratorNotification(Scp079Generator generator, bool skipAnimation)
		: base($"<color=red>0m 00s - {Scp079GeneratorNotification.GetGeneratorCamera(generator)}</color>")
	{
		this._activeStopwatch = (skipAnimation ? new Stopwatch() : Stopwatch.StartNew());
		this._emptyBuilder = new StringBuilder();
		this._generator = generator;
		this._opacity = 1f;
	}

	private static void OverrideStringBuilder(StringBuilder sb, int place, int character)
	{
		if (sb.Length > place)
		{
			sb[place] = (char)character;
		}
	}

	private static string GetGeneratorCamera(Scp079Generator gen)
	{
		Vector3 position = gen.transform.position;
		if (!position.TryGetRoom(out var room))
		{
			return "UNKNOWN";
		}
		bool flag = false;
		float num = 0f;
		Scp079Camera scp079Camera = null;
		Scp079Camera scp079Camera2 = null;
		foreach (Scp079InteractableBase allInstance in Scp079InteractableBase.AllInstances)
		{
			if (allInstance is Scp079Camera scp079Camera3 && scp079Camera3.Room == room)
			{
				float sqrMagnitude = (scp079Camera3.Position - position).sqrMagnitude;
				if (scp079Camera3.IsMain)
				{
					scp079Camera2 = scp079Camera3;
				}
				if ((!flag || !(sqrMagnitude > num)) && (!Physics.Linecast(scp079Camera3.Position, position, out var hitInfo, 1) || !(hitInfo.collider.GetComponentInParent<Scp079Generator>() != gen)))
				{
					scp079Camera = scp079Camera3;
					flag = true;
					num = sqrMagnitude;
				}
			}
		}
		if (!flag)
		{
			if (!(scp079Camera2 != null))
			{
				return "UNKNOWN";
			}
			return scp079Camera2.Label;
		}
		return scp079Camera.Label;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub x, PlayerRoleBase y, PlayerRoleBase z)
		{
			if (x.isLocalPlayer)
			{
				Scp079GeneratorNotification.TrackedGens.Clear();
			}
		};
	}
}
