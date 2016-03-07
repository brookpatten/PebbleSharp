using DBus;
using Mono.BlueZ.DBus;

namespace PebbleSharp.Mono.BlueZ5
{
	internal class PebbleAgent:Agent1
	{
		public PebbleAgent()
		{
		}
		public void Release()
		{
		}
		public string RequestPinCode(ObjectPath device)
		{
			return "1";
		}
		public void DisplayPinCode(ObjectPath device,string pinCode)
		{
		}
		public uint RequestPasskey(ObjectPath device)
		{
			return 1;
		}
		public void DisplayPasskey (ObjectPath device, uint passkey, ushort entered)
		{
		}
		public void RequestConfirmation(ObjectPath device,uint passkey)
		{
		}
		public void RequestAuthorization(ObjectPath device)
		{
		}
		public void AuthorizeService(ObjectPath device,string uuid)
		{
		}
		public void Cancel()
		{
		}
	}
}

