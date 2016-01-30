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
	public class PebbleManager:IDisposable
    {
		const string Service = "org.bluez";
		private readonly ObjectPath AgentPath = new ObjectPath ("/agent");
		private readonly ObjectPath ProfilePath = new ObjectPath ("/profiles");
		private readonly ObjectPath BlueZPath = new ObjectPath ("/org/bluez");
		const string PebbleSerialUUID = "00000000-deca-fade-deca-deafdecacaff";

		private ObjectManager ObjectManager;
		private ProfileManager1 ProfileManager;
		private PebbleProfile Profile;
		private AgentManager1 AgentManager;
		private PebbleAgent Agent;
		private Adapter1 Adapter;
		private Dictionary<ObjectPath,DiscoveredPebble> Pebbles;

		private bool _ownsConnection = false;
		private DBusConnection _connection;

		public PebbleManager():this(new DBusConnection())
		{
			_ownsConnection = true;
		}

		public PebbleManager(DBusConnection connection)
		{
			_ownsConnection = false;
			Pebbles = new Dictionary<ObjectPath, DiscoveredPebble> ();
			_connection = connection;
		}

		public IList<Pebble> Detect(string adapterName,bool doDiscovery)
		{
			//these properties are defined by bluez in /doc/profile-api.txt
			//but it turns out the defaults work just fine
			var properties = new Dictionary<string,object> ();
			//properties ["AutoConnect"] = true;
			//properties ["Name"] = "Serial Port";
			//properties ["Service"] = pebbleSerialUUID;
			//properties ["Role"] = "client";
			//properties ["PSM"] = (ushort)1;
			//properties ["RequireAuthentication"] = false;
			//properties ["RequireAuthorization"] = false;
			//properties ["Channel"] = (ushort)0;

			//get a proxy for the profile manager so we can register our profile
			ProfileManager =  _connection.System.GetObject<ProfileManager1> (Service, BlueZPath);
			//create and register our profile
			Profile = new PebbleProfile ();
			_connection.System.Register (ProfilePath, Profile);
			ProfileManager.RegisterProfile (ProfilePath
				, PebbleSerialUUID
				, properties);
			Profile.NewConnectionAction=(path,stream,props)=>{
				if(Pebbles.ContainsKey(path))
				{
					Pebbles[path].FileDescriptor = stream;
				}
			};

			//get a copy of the object manager so we can browse the "tree" of bluetooth items
			ObjectManager = _connection.System.GetObject<org.freedesktop.DBus.ObjectManager> (Service, ObjectPath.Root);
			//register these events so we can tell when things are added/removed (eg: discovery)
			ObjectManager .InterfacesAdded += (p, i) => {
				System.Console.WriteLine (p + " Discovered");
			};
			ObjectManager .InterfacesRemoved += (p, i) => {
				System.Console.WriteLine (p + " Lost");
			};

			//get the agent manager so we can register our agent
			AgentManager = _connection.System.GetObject<AgentManager1> (Service, BlueZPath);
			Agent = new PebbleAgent();
			//register our agent and make it the default
			_connection.System.Register (AgentPath, Agent);
			AgentManager.RegisterAgent (AgentPath, "KeyboardDisplay");
			AgentManager.RequestDefaultAgent (AgentPath);

			//get the bluetooth object tree
			var managedObjects = ObjectManager.GetManagedObjects();
			//find our adapter
			ObjectPath adapterPath = null;
			foreach (var obj in managedObjects.Keys) {
				if (managedObjects [obj].ContainsKey (typeof(Adapter1).DBusInterfaceName ())) {
					if (string.IsNullOrEmpty(adapterName) || obj.ToString ().EndsWith (adapterName)) {
						adapterPath = obj;
						break;
					}
				}
			}

			if (adapterPath == null) 
			{
				throw new ArgumentException("Could not find bluetooth adapter");
			}

			//get a dbus proxy to the adapter
			Adapter = _connection.System.GetObject<Adapter1> (Service, adapterPath);

			if(doDiscovery)
			{
				//scan for any new devices
				Adapter.StartDiscovery ();
				Thread.Sleep(5000);//totally arbitrary constant, the best kind
				//Thread.Sleep ((int)adapter.DiscoverableTimeout * 1000);

				//refresh the object graph to get any devices that were discovered
				//arguably we should do this in the objectmanager added/removed events and skip the full
				//refresh, but I'm lazy.
				managedObjects = ObjectManager.GetManagedObjects();
			}

			foreach (var obj in managedObjects.Keys) 
			{
				if (obj.ToString ().StartsWith (adapterPath.ToString ())) 
				{
					if (managedObjects [obj].ContainsKey (typeof(Device1).DBusInterfaceName ())) 
					{
						var managedObject = managedObjects [obj];
						var name = (string)managedObject[typeof(Device1).DBusInterfaceName()]["Name"];

						if (name.StartsWith ("Pebble")) 
						{
							System.Console.WriteLine ("Device " + name + " at " + obj);
							var device = _connection.System.GetObject<Device1> (Service, obj);

							try
							{
								if (!device.Paired) {
									device.Pair ();
								}
								if (!device.Trusted) {
									device.Trusted=true;
								}
								device.ConnectProfile(PebbleSerialUUID);
								Pebbles[obj]=new DiscoveredPebble(){Name=name,Device = device};
							}
							catch(Exception ex)
							{
								//we don't need to do anything, it simply won't be added to the collection if we can't connect to it
							}
						}
					}
				}
			}
			//wait for devices to connect
			Thread.Sleep(2000);

			var results = new List<Pebble>();
			foreach(var path in Pebbles.Keys)
			{
				if(Pebbles[path].FileDescriptor!=null)
				{
					if(Pebbles[path].Pebble==null)
					{
						Pebbles[path].FileDescriptor.SetBlocking();
						var stream = Pebbles[path].FileDescriptor.OpenAsStream(true);
						Pebbles[path].Stream=stream;
						var blueZPebble = new BlueZ5Pebble(new PebbleBluetoothConnection(stream),Pebbles[path].Name);
						Pebbles[path].Pebble = blueZPebble;
						results.Add(blueZPebble);
					}
					else
					{
						results.Add(Pebbles[path].Pebble);
					}
				}
			}
			return results;
		}

		private class DiscoveredPebble
		{
			public Device1 Device{ get; set; }
			public FileDescriptor FileDescriptor{get;set;}
			public string Name{get;set;}
			public Stream Stream{get;set;}
			public BlueZ5Pebble Pebble{ get; set; }
		}

		public void Dispose()
		{
			if (AgentManager != null && AgentPath != null)
			{
				AgentManager.UnregisterAgent (AgentPath);
			}
			if (ProfileManager != null && ProfilePath != null)
			{
				ProfileManager.UnregisterProfile (ProfilePath);
			}
			if (_ownsConnection && _connection != null)
			{
				_connection.Dispose ();
			}
		}
	}
}