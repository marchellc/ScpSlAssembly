using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore
{
	public class RoundStart : NetworkBehaviour
	{
		public static bool RoundStarted
		{
			get
			{
				return RoundStart._singletonSet && RoundStart.singleton.Timer == -1;
			}
		}

		public static TimeSpan RoundLength
		{
			get
			{
				return RoundStart.RoundStartTimer.Elapsed;
			}
		}

		static RoundStart()
		{
			SceneManager.sceneLoaded += RoundStart.OnSceneLoaded;
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			RoundStart.RoundStartTimer.Reset();
		}

		private void Start()
		{
			base.GetComponent<RectTransform>().localPosition = Vector3.zero;
		}

		private void Awake()
		{
			RoundStart.singleton = this;
			RoundStart._singletonSet = true;
		}

		private void OnDestroy()
		{
			RoundStart._singletonSet = false;
		}

		private void Update()
		{
		}

		public override bool Weaved()
		{
			return true;
		}

		public short NetworkTimer
		{
			get
			{
				return this.Timer;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<short>(value, ref this.Timer, 1UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteShort(this.Timer);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteShort(this.Timer);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<short>(ref this.Timer, null, reader.ReadShort());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<short>(ref this.Timer, null, reader.ReadShort());
			}
		}

		public static RoundStart singleton;

		public static bool LobbyLock;

		private static bool _singletonSet;

		[SyncVar]
		public short Timer = -2;

		internal static readonly Stopwatch RoundStartTimer = new Stopwatch();
	}
}
