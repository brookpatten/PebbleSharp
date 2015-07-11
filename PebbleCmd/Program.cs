
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PebbleSharp.Core;
using PebbleSharp.Core.Responses;
using PebbleSharp.Net45;
using InTheHand.Net.Bluetooth;
using PebbleSharp.Core.Bundles;
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
                //check if BlueZ support is enabled
                Console.WriteLine("Radio Software Manufacturer {0}", BluetoothRadio.PrimaryRadio.SoftwareManufacturer);


                Console.WriteLine("PebbleCmd");
                Console.WriteLine("Select Pebble to connect to:");
                var pebbles = PebbleNet45.DetectPebbles();
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
                        Console.WriteLine("Connecting to Pebble " + selectedPebble.PebbleID);
						selectedPebble.ConnectAsync().Wait();
                        Console.WriteLine("Connected");
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
                "Send App Message");
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


                        using (var stream = new FileStream("../../../AppMessageTest.pbw", FileMode.Open))
                        {
                            using (var zip = new Zip())
                            {
                                zip.Open(stream);
                                var bundle = new AppBundle();
                                bundle.Load(stream, zip);
                                var task = pebble.InstallAppAsync(bundle, progress);
                                await task;
                                Console.WriteLine("App Installed");
                            }
                        }
                        break;
                    case 7:
                        //read the uuid from the pbw
                        UUID uuid=null;
                        using (var stream = new FileStream("../../../AppMessageTest.pbw", FileMode.Open))
                        {
                            using (var zip = new Zip())
                            {
                                zip.Open(stream);
                                var bundle = new AppBundle();
                                bundle.Load(stream, zip);
                                uuid = bundle.AppMetadata.UUID;
                            }
                        }

                        //format a message
                        AppMessageDictionary message = new AppMessageDictionary();
                        message.Values.Add(new AppMessageUInt32(){Value = (uint)new Random().Next()});

                        //send it
                        var t = pebble.SendApplicationMessage(uuid, message);
                        await t;
                        Console.WriteLine("Response received");
                        break;
                }
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
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
