using Mirror;
using UnityEngine;

namespace PlayerRoles.Subroutines;

public class SubroutineManagerModule : MonoBehaviour
{
	public SubroutineBase[] AllSubroutines;

	private void OnValidate()
	{
		this.AllSubroutines = base.GetComponentsInChildren<SubroutineBase>();
	}

	public bool TryGetSubroutine<T>(out T subroutine) where T : SubroutineBase
	{
		for (int i = 0; i < this.AllSubroutines.Length; i++)
		{
			if (this.AllSubroutines[i] is T val)
			{
				subroutine = val;
				return true;
			}
		}
		subroutine = null;
		return false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkServer.ReplaceHandler(delegate(NetworkConnectionToClient conn, SubroutineMessage msg)
			{
				msg.ServerApplyTrigger(conn);
			});
			NetworkClient.ReplaceHandler(delegate(NetworkConnection conn, SubroutineMessage msg)
			{
				msg.ClientApplyConfirmation();
			});
		};
	}
}
