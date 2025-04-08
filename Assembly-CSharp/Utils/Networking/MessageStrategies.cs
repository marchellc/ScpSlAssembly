using System;
using Mirror;

namespace Utils.Networking
{
	public static class MessageStrategies<TSelf>
	{
		public static MessageStrategies<TSelf>.MessageHandler<TMessage> ToClient<TMessage>(MessageStrategies<TSelf>.MessageHandler<TMessage> handler, MessageStrategies<TSelf>.SelfToIdentity converter) where TMessage : struct, NetworkMessage
		{
			return delegate(TSelf self, TMessage message)
			{
				NetworkIdentity networkIdentity = converter(self);
				if (networkIdentity.isLocalPlayer)
				{
					handler(self, message);
					return;
				}
				networkIdentity.connectionToClient.Send<TMessage>(message, 0);
			};
		}

		public static void RegisterFromClient<TMessage>(MessageStrategies<TSelf>.MessageHandler<TMessage> handler, MessageStrategies<TSelf>.ConnectionToSelf converter) where TMessage : struct, NetworkMessage
		{
			NetworkServer.ReplaceHandler<TMessage>(delegate(NetworkConnectionToClient connection, TMessage message)
			{
				TSelf tself;
				if (converter(connection, out tself))
				{
					handler(tself, message);
				}
			}, true);
		}

		public static MessageStrategies<TSelf>.MessageHandler<TMessage> ToServer<TMessage>(MessageStrategies<TSelf>.MessageHandler<TMessage> handler, MessageStrategies<TSelf>.SelfToIdentity converter) where TMessage : struct, NetworkMessage
		{
			return delegate(TSelf self, TMessage message)
			{
				if (converter(self).isLocalPlayer)
				{
					handler(self, message);
					return;
				}
				NetworkClient.Send<TMessage>(message, 0);
			};
		}

		public static void RegisterFromServer<TMessage>(MessageStrategies<TSelf>.MessageHandler<TMessage> handler, TSelf self) where TMessage : struct, NetworkMessage
		{
			NetworkClient.ReplaceHandler<TMessage>(delegate(NetworkConnection connection, TMessage message)
			{
				handler(self, message);
			}, true);
		}

		public delegate NetworkIdentity SelfToIdentity(TSelf self);

		public delegate bool ConnectionToSelf(NetworkConnection source, out TSelf self);

		public delegate void MessageHandler<in TMessage>(TSelf self, TMessage message);
	}
}
