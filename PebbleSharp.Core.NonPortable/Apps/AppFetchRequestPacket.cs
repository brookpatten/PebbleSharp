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

			//TODO: figure out why this is fubar, something something endianness
			//packet capture from libpebble 2 shows the endians being flipped.... somewhere.
			//possibly a python thing?  I have no idea.
			var bytes = new byte[]{ payload[20], payload[19], payload[18], payload[17] };

			AppId = Util.GetInt32(bytes,0);
		}
	}
}

