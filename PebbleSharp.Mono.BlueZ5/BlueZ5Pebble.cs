using PebbleSharp.Core;

namespace PebbleSharp.Mono.BlueZ5
{
	public class BlueZ5Pebble : Pebble
	{
		internal BlueZ5Pebble( PebbleBluetoothConnection connection, string pebbleId ): base( connection, pebbleId )
		{ 
		}
	}
}

