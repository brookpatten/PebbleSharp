using PebbleSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;

namespace PebbleSharp.Mono.BlueZ5
{
	public class PebbleManager:IDisposable
    {
		private readonly ObjectPath AgentPath = new ObjectPath ("/agent");
		private readonly ObjectPath ProfilePath = new ObjectPath ("/profiles");
		const string PebbleSerialUUID = "00000000-deca-fade-deca-deafdecacaff";

		private ObjectManager _objectManager;
		private ProfileManager1 _profileManager;
		private PebbleProfile _profile;
		private AgentManager1 _agentManager;
		private PebbleAgent _agent;
		private Adapter1 _adapter;
		private Dictionary<ObjectPath,DiscoveredPebble> _pebbles;

		private bool _ownsConnection = false;
		private DBusConnection _connection;

		public PebbleManager():this(new DBusConnection())
		{
			_ownsConnection = true;
		}

		public PebbleManager(DBusConnection connection)
		{
			_ownsConnection = false;
			_pebbles = new Dictionary<ObjectPath, DiscoveredPebble> ();
			_connection = connection;
		}

		public IList<Pebble> Detect(string adapterName,bool doDiscovery,bool pairDiscovered,bool unpairFailed)
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
			_profileManager =  _connection.System.GetObject<ProfileManager1> (BlueZPath.Service, BlueZPath.Root);
			//create and register our profile
			_profile = new PebbleProfile ();
			_connection.System.Register (ProfilePath, _profile);
			_profileManager.RegisterProfile (ProfilePath
				, PebbleSerialUUID
				, properties);
			_profile.NewConnectionAction=(path,fd,props)=>{
				//System.Console.WriteLine("Connected to " + path);
				_pebbles[path].FileDescriptor = fd;
				_pebbles[path].FileDescriptor.SetBlocking();
				var stream = _pebbles[path].FileDescriptor.OpenAsStream(true);
				_pebbles[path].Stream=stream;
				var blueZPebble = new BlueZ5Pebble(new PebbleBluetoothConnection(stream),_pebbles[path].Name);
				_pebbles[path].Pebble = blueZPebble;
			};

			//get a copy of the object manager so we can browse the "tree" of bluetooth items
			_objectManager = _connection.System.GetObject<org.freedesktop.DBus.ObjectManager> (BlueZPath.Service, ObjectPath.Root);
			//register these events so we can tell when things are added/removed (eg: discovery)
			//_objectManager .InterfacesAdded += (p, i) => {
				//System.Console.WriteLine ("Discovered "+p);
			//};
			//_objectManager .InterfacesRemoved += (p, i) => {
				//System.Console.WriteLine ("Lost" + p);
			//};

			//get the agent manager so we can register our agent
			_agentManager = _connection.System.GetObject<AgentManager1> (BlueZPath.Service, BlueZPath.Root);
			_agent = new PebbleAgent();
			//register our agent and make it the default
			_connection.System.Register (AgentPath, _agent);
			_agentManager.RegisterAgent (AgentPath, "KeyboardDisplay");
			_agentManager.RequestDefaultAgent (AgentPath);

			//get the bluetooth object tree
			var managedObjects = _objectManager.GetManagedObjects();
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
			_adapter = _connection.System.GetObject<Adapter1> (BlueZPath.Service, BlueZPath.Adapter(adapterName));

			if(doDiscovery)
			{
				System.Console.WriteLine("Starting Discovery...");
				//scan for any new devices
				_adapter.StartDiscovery ();
				Thread.Sleep(5000);//totally arbitrary constant, the best kind
				//Thread.Sleep ((int)adapter.DiscoverableTimeout * 1000);

				//refresh the object graph to get any devices that were discovered
				//arguably we should do this in the objectmanager added/removed events and skip the full
				//refresh, but I'm lazy.
				managedObjects = _objectManager.GetManagedObjects();
			}

			foreach (var obj in managedObjects.Keys) 
			{
				if (obj.ToString ().StartsWith (adapterPath.ToString ())) 
				{
					if (managedObjects [obj].ContainsKey (typeof(Device1).DBusInterfaceName ())) 
					{
						var managedObject = managedObjects [obj];
						if (managedObject [typeof(Device1).DBusInterfaceName ()].ContainsKey ("Name"))
						{
							var name = (string)managedObject [typeof(Device1).DBusInterfaceName ()] ["Name"];
							if (name.StartsWith ("Pebble") && !name.Contains(" LE "))
							{
								var device = _connection.System.GetObject<Device1> (BlueZPath.Service, obj);
								//we also check for the UUID because that's how we tell the 
								//LE address from the regular address

								try 
								{
									System.Console.WriteLine ("Attempting connection to " + obj);
									if (!device.Paired && pairDiscovered) 
									{
										device.Pair ();
									}
									if (!device.Trusted && pairDiscovered) 
									{
										device.Trusted = true;
									}
									_pebbles [obj] = new DiscoveredPebble () { Name = name, Device = device };

									try 
									{
										device.ConnectProfile (PebbleSerialUUID);
									} 
									catch (Exception ex) 
									{
										if (unpairFailed) {
											//this prevents us from falling into a failing loop
											//if the pebble has unpaired but bluez has not

											System.Console.WriteLine ("Failed to connect to " + obj + ", attempting to re-pair");
											//if we can't connect then try to re-pair then reconnect
											_adapter.RemoveDevice (obj);

											if (pairDiscovered) {
												//re-discover
												if (!_adapter.Discovering) {
													_adapter.StartDiscovery ();
												}
												System.Threading.Thread.Sleep (10000);
												//re-pair
												device.Pair ();
												device.Trusted = true;
												//re-connect
												device.ConnectProfile (PebbleSerialUUID);
											}
										} 
										else 
										{
											throw;
										}
									}
								} 
								catch (Exception ex) 
								{
									System.Console.WriteLine ("Failed to connect to " + obj + " " + ex.Message);
									//we don't need to do anything, it simply won't be added to the collection if we can't connect to it
								}
							}
						}
						//if it doesn't have the Name Property we can safely assume it is not a pebble
					}
				}
			}
			//wait for devices to connect
			Thread.Sleep(2000);

			var results = _pebbles.Values.Where(x => x.Pebble != null).Select(x => (Pebble)x.Pebble).ToList();
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
			//don't think we need these, bluez will cleanup all our objects once we disconnect
			//if (_agentManager != null && AgentPath != null)
			//{
			//	_agentManager.UnregisterAgent (AgentPath);
			//}
			//if (_profileManager != null && ProfilePath != null)
			//{
			//	_profileManager.UnregisterProfile (ProfilePath);
			//}
			if (_ownsConnection && _connection != null)
			{
				_connection.Dispose ();
			}
		}
	}
}