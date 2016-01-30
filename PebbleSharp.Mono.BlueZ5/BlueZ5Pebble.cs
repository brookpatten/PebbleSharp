using PebbleSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;
using System.IO;

namespace PebbleSharp.Mono.BlueZ5
{
	public class BlueZ5Pebble : Pebble
	{
		internal BlueZ5Pebble( PebbleBluetoothConnection connection, string pebbleId ): base( connection, pebbleId )
		{ 
		}
	}
}

