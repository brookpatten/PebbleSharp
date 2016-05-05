using PebbleSharp.Core;
using Mono.BlueZ.DBus;

namespace PebbleSharp.Mono.BlueZ5
{
	public class BlueZ5Pebble : Pebble
	{
		private Device1 _device;

		internal BlueZ5Pebble( PebbleBluetoothConnection connection,Device1 device, string pebbleId ): base( connection, pebbleId )
		{
			_device = device;
		}

		public override void Reconnect ()
		{
			if (!_device.Connected) 
			{
				_device.ConnectProfile (PebbleManager.PebbleSerialUUID);
				IsAlive = true;
			}
		}
	}
}

