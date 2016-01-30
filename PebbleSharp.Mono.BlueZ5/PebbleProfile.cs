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
	internal class PebbleProfile:Profile1
	{
		private FileDescriptor _fileDescriptor;

		public Action<ObjectPath,FileDescriptor,IDictionary<string,object>> NewConnectionAction{get;set;}
		public Action<ObjectPath,FileDescriptor> RequestDisconnectionAction{ get; set; }
		public Action<FileDescriptor> ReleaseAction{ get; set; }

		public PebbleProfile ()
		{
		}

		public void Release ()
		{
			if (ReleaseAction != null) {
				ReleaseAction (_fileDescriptor);
			}
		}
		public void NewConnection (ObjectPath device, FileDescriptor fileDescriptor, IDictionary<string,object> properties)
		{
			_fileDescriptor = fileDescriptor;
			if (NewConnectionAction != null) {
				NewConnectionAction (device, _fileDescriptor, properties);
			}
		}
		public void RequestDisconnection (ObjectPath device)
		{
			if (RequestDisconnectionAction != null) {
				RequestDisconnectionAction (device, _fileDescriptor);
			} else {
				if (_fileDescriptor != null) {
					_fileDescriptor.Close ();
				}
			}
		}
	}
}

