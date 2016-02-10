using System;
using System.Linq;
using PebbleSharp.Core.Responses;
namespace PebbleSharp.Core
{
	[Endpoint(Endpoint.AppFetch)]
	public class AppFetchRequestPacket:ResponseBase
	{
		public byte Command { get; set; }
		public UUID UUID { get; set; }
		public int AppId { get; set; }

		public AppFetchRequestPacket()
		{
		}

		protected override void Load(byte[] payload)
		{
			Command = payload[0];
			UUID = new UUID(payload.Skip(1).Take(16).ToArray());
			AppId = BitConverter.ToInt32(payload, 17);
		}
	}
}

