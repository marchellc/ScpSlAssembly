using Mirror;

namespace VoiceChat.Networking;

public struct VoiceMessage : NetworkMessage
{
	public ReferenceHub Speaker;

	public VoiceChatChannel Channel;

	public int DataLength;

	public byte[] Data;

	public bool SpeakerNull;

	public VoiceMessage(ReferenceHub ply, VoiceChatChannel channel, byte[] data, int dataLen, bool isNull)
	{
		this.Speaker = ply;
		this.Channel = channel;
		this.Data = data;
		this.DataLength = dataLen;
		this.SpeakerNull = isNull;
	}
}
