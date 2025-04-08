using System;
using System.Collections.Generic;
using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication
{
	public class CommunicationProcessor
	{
		public static T RequestServerChannel<T>() where T : IServerCommunication
		{
			foreach (IServerCommunication serverCommunication in CommunicationProcessor.ServerCommunication.Values)
			{
				if (serverCommunication is T)
				{
					return (T)((object)serverCommunication);
				}
			}
			return default(T);
		}

		public static readonly Dictionary<int, IServerCommunication> ServerCommunication = new Dictionary<int, IServerCommunication>
		{
			{
				0,
				new RaPlayerList()
			},
			{
				1,
				new RaPlayer()
			},
			{
				3,
				new RaPlayerAuth()
			},
			{
				5,
				new RaGlobalBan()
			},
			{
				7,
				new RaServerStatus()
			},
			{
				8,
				new RaTeamStatus()
			}
		};
	}
}
