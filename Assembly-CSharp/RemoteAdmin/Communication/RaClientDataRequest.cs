using System.Text;
using RemoteAdmin.Interfaces;

namespace RemoteAdmin.Communication;

public abstract class RaClientDataRequest : IServerCommunication, IClientCommunication
{
	private readonly StringBuilder _stringBuilder = new StringBuilder();

	public abstract int DataId { get; }

	public virtual void ReceiveData(string data, bool secure)
	{
	}

	public virtual void ReceiveData(CommandSender sender, string data)
	{
		this._stringBuilder.Clear();
		this._stringBuilder.Append("$").Append(this.DataId).Append(" ");
		this.GatherData();
		sender.RaReply($"${this.DataId} {this._stringBuilder}", success: true, logToConsole: false, string.Empty);
	}

	protected abstract void GatherData();

	protected void AppendData(object data)
	{
		this._stringBuilder.Append(data).Append(",");
	}

	protected int CastBool(bool value)
	{
		if (!value)
		{
			return 0;
		}
		return 1;
	}
}
