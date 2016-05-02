
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PebbleSharp;

using PebbleSharp.Core;
using PebbleSharp.Mono.BlueZ5;
using PebbleSharp.Core.Bundles;
using PebbleSharp.Core.BlobDB;
using PebbleSharp.Core.Responses;
using PebbleSharp.Core.NonPortable.AppMessage;

namespace PebbleCmd
{
    /// <summary>
    /// A simple console application for testing messages with the Pebble watch.
    /// </summary>
    class Program
    {
        //static void Main()
        //{
            //ShowPebbles().Wait();
        //}

        //private static async Task ShowPebbles()
		static void Main()
		{
            try
            {
				var manager = new PebbleManager();
                Console.WriteLine("PebbleCmd");
                Console.WriteLine("Discovering and Pairing Pebbles");
				var pebbles = manager.Detect("hci1",true,true,true);
				Console.WriteLine("Select Pebble to connect to:");
                if (pebbles != null && pebbles.Any())
                {
					int result =0;

					if(pebbles.Count>1)
					{
                    	var options = pebbles.Select(x => x.PebbleID).Union(new[] { "Exit" });
                    	var menu = new Menu(options.ToArray());
                    	result = menu.ShowMenu();
					}

                    if (result >= 0 && result < pebbles.Count)
                    {
                        var selectedPebble = pebbles[result];
                        Console.WriteLine("Connecting to " + selectedPebble.PebbleID);
						selectedPebble.ConnectAsync().Wait();
                        ShowPebbleMenu(selectedPebble).Wait();
                    }
                }
                else
                {
                    Console.WriteLine("No Pebbles Detected");
                }
            }
            catch(Exception ex)
            {

				Console.WriteLine(ex.Message + " " + ex.StackTrace);
                throw;
            }
        }

        private static string SelectApp()
        {
			string exePath = System.Reflection.Assembly.GetExecutingAssembly ().CodeBase;
			if (exePath.StartsWith ("file:"))
			{
				exePath = exePath.Substring (5);
			}
			string exeDir = Path.GetDirectoryName (exePath);
			var dir = new DirectoryInfo (exeDir);
			var files = dir.GetFiles ("*.pbw");

            if (files.Any())
            {
                if (files.Count() == 1)
                {
                    return files.Single().FullName;
                }
                else
                {
                    var fileMenu = new Menu(files.Select(x => x.Name).ToArray());
                    int index = fileMenu.ShowMenu();
                    return files[index].FullName;
                }
            }
            else
            {
                Console.WriteLine("No .pbw files found");
                return null;
            }
        }



        private static async Task ShowPebbleMenu( Pebble pebble )
        {
            //string uuid = "22a27b9a-0b07-47af-ad87-b2c29305bab6";
            
            var menu = new Menu(
                "Disconnect",
                "Get Time",
                "Set Current Time",
                "Get Firmware Info",
                "Send Ping",
                "Media Commands",
                "Install App",
                "Send App Message",
				"Reset",
				"Send Notification");
            while ( true )
            {
                switch (menu.ShowMenu())
                {
                    case 0:
                        pebble.Disconnect();
                        return;
                    case 1:
                        var timeResult = await pebble.GetTimeAsync();
                        DisplayResult(timeResult, x => string.Format("Pebble Time: " + x.Time.ToString("G")));
                        break;
                    case 2:
                        await pebble.SetTimeAsync(DateTime.Now);
                        goto case 1;
                    case 3:
                        var firmwareResult = await pebble.GetFirmwareVersionAsync();
                        DisplayResult(firmwareResult,
                            x => string.Join(Environment.NewLine, "Firmware", x.Firmware.ToString(),
                                "Recovery Firmware", x.RecoveryFirmware.ToString()));
                        break;
                    case 4:
                        var pingResult = await pebble.PingAsync();
                        DisplayResult(pingResult, x => "Received Ping Response");
                        break;
                    case 5:
                        ShowMediaCommands(pebble);
                        break;
                    case 6:
                        var progress =
                            new Progress<ProgressValue>(
                                pv => Console.WriteLine(pv.ProgressPercentage + " " + pv.Message));

                        string appPath = SelectApp();

                        if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                        {
                            using (var stream = new FileStream(appPath, FileMode.Open))
                            {
                                using (var zip = new Zip())
                                {
                                    zip.Open(stream);
                                    var bundle = new AppBundle();
                                    stream.Position = 0;
									bundle.Load(zip,pebble.Firmware.HardwarePlatform.GetPlatform());
                                    var task = pebble.InstallClient.InstallAppAsync(bundle, progress);
                                    await task;
									if (task.IsFaulted)
									{
										Console.WriteLine("Failed to install");
									}

									//for firmware v3, launch is done as part of the install
                                    //Console.WriteLine("App Installed, launching...");
									//var uuid=new UUID(bundle.AppInfo.UUID);
									//pebble.LaunchApp(uuid);
									//Console.WriteLine ("Launched");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("No .pbw");
                        }
                        break;
                    case 7:
                        //read the uuid from the pbw

                        string uuidAppPath = SelectApp();

                        if (!string.IsNullOrEmpty(uuidAppPath) && File.Exists(uuidAppPath))
                        {
							using (var stream = new FileStream(uuidAppPath, FileMode.Open))
							{
								using (var zip = new Zip())
								{
									zip.Open(stream);
									var bundle = new AppBundle();
									stream.Position = 0;
									bundle.Load(zip,pebble.Firmware.HardwarePlatform.GetPlatform());


									//format a message
									var rand = new Random().Next();
									AppMessagePacket message = new AppMessagePacket();
									message.Values.Add(new AppMessageUInt32() { Value = (uint)rand });
									message.Values.Add(new AppMessageString() { Value = "Hello from .net" });
									message.ApplicationId = bundle.AppMetadata.UUID;
									message.TransactionId = 255;


									//send it
									Console.WriteLine("Sending Status "+rand+" to " + bundle.AppMetadata.UUID.ToString());
									var t = pebble.SendApplicationMessage(message);
									await t;
									Console.WriteLine("Response received");
								}
							}



                        }
                        else
                        {
                            Console.WriteLine("No .pbw");
                        }
						break;
					case 8:
						pebble.Reset(ResetCommand.Reset);
						break;
					case 9:
						TestNotification(pebble);
						break;
                }
            }
        }

		private static void TestNotification(Pebble pebble)
		{
			var item = new TimelineItem()
			{
				ItemId = new UUID("22a27b9a-0b07-47af-ad87-b2c29305bab6"),
				ParentId = new UUID(new byte[16]),
				TimeStamp = DateTime.UtcNow,
				Duration = 0,
				ItemType = TimelineItem.TimelineItemType.Notification,
				Flags = 0,
				Layout = 0x01,
				Attributes = new List<TimelineAttribute>() {
					new TimelineAttribute(){
						AttributeId=0x01,
						Content=Util.GetBytes("MrGibbs",false)
					},
					new TimelineAttribute(){
						AttributeId=4,
						Content=BitConverter.GetBytes((uint)45)
					},
					new TimelineAttribute(){
						AttributeId=0x03,
						Content=Util.GetBytes("Hello world!",false)
					},
					new TimelineAttribute{
						AttributeId=0x02,
						Content=Util.GetBytes("Subject",false)
					}
				},
				Actions = new List<TimelineAction>() { 
					new TimelineAction(){
						ActionId=0,
						ActionType = TimelineAction.TimelineActionType.Dismiss,
						Attributes = new List<TimelineAttribute>(){
							new TimelineAttribute(){
								AttributeId=0x01,
								Content=Util.GetBytes("Dismiss",false)
							}
						}
					}
				}
			};

			/*source_map = {
            None: 1,
            NotificationSource.Email: 19,
            NotificationSource.Facebook: 11,
            NotificationSource.SMS: 45,
            NotificationSource.Twitter: 6,
        	})*/

			var bytes = item.GetBytes();
			var task = pebble.BlobDBClient.Insert(BlobDatabase.Notification, item.ItemId.Data, bytes);
			task.Wait();
			var result = task.Result;

			if (result.Response == BlobStatus.Success)
			{
				System.Console.Write("Insert Success, Deleting in ");
				for (int i = 5; i > 0; i--)
				{
					System.Console.Write(i + "...");
					System.Threading.Thread.Sleep(1000);
				}
				System.Console.WriteLine();

				task = pebble.BlobDBClient.Delete(BlobDatabase.Notification, item.ItemId.Data);
				task.Wait();
				result = task.Result;

				if (result.Response == BlobStatus.Success)
				{
					System.Console.WriteLine("Delete Success");
				}
				else
				{
					System.Console.WriteLine("Delete Failed: " + result.Response);
				}
			}
			else
			{
				System.Console.WriteLine("Insert Failed:" + result.Response.ToString()+" with token "+result.Token);
			}
		}

		private static void TestBlobDB(Pebble pebble)
		{
			
		}
        

        private static void ShowMediaCommands( Pebble pebble )
        {
            Console.WriteLine( "Listening for media commands" );
            pebble.RegisterCallback<MusicControlResponse>( result =>
                DisplayResult( result, x => string.Format( "Media Control Response " + x.Command ) ) );

            var menu = new Menu( "Return to menu" );

            while ( true )
            {
                switch ( menu.ShowMenu() )
                {
                    case 0:
                        return;
                }
            }
        }

        private static void DisplayResult<T>( T result, Func<T, string> successData )
            where T : ResponseBase
        {
            if ( result.Success )
            {
                Console.WriteLine( successData( result ) );
            }
            else
            {
                Console.WriteLine( "ERROR" );
                Console.WriteLine( result.ErrorMessage );
                Console.WriteLine( result.ErrorDetails.ToString() );
            }
        }
    }
}
