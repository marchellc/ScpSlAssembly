using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.Subroutines
{
	public class SubroutineManagerModule : MonoBehaviour
	{
		private void OnValidate()
		{
			this.AllSubroutines = base.GetComponentsInChildren<SubroutineBase>();
		}

		public bool TryGetSubroutine<T>(out T subroutine) where T : SubroutineBase
		{
			for (int i = 0; i < this.AllSubroutines.Length; i++)
			{
				T t = this.AllSubroutines[i] as T;
				if (t != null)
				{
					subroutine = t;
					return true;
				}
			}
			subroutine = default(T);
			return false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<SubroutineMessage>(delegate(NetworkConnectionToClient conn, SubroutineMessage msg)
				{
					msg.ServerApplyTrigger(conn);
				}, true);
				NetworkClient.ReplaceHandler<SubroutineMessage>(delegate(NetworkConnection conn, SubroutineMessage msg)
				{
					msg.ClientApplyConfirmation();
				}, true);
			};
		}

		public SubroutineBase[] AllSubroutines;
	}
}
