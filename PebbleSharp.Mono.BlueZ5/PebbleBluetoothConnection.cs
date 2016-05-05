using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using PebbleSharp.Core;

namespace PebbleSharp.Mono.BlueZ5
{
	internal sealed class PebbleBluetoothConnection : IBluetoothConnection, IDisposable
	{
		private CancellationTokenSource _tokenSource;

		private Stream _networkStream;
		public event EventHandler<BytesReceivedEventArgs> DataReceived = delegate { };

		public PebbleBluetoothConnection( Stream Stream)
		{
			_networkStream=Stream;
			//_deviceInfo = deviceInfo;
		}

		public void Reconnect (Stream stream)
		{
			_networkStream = stream;
			OpenAsync ().Wait ();
		}

		~PebbleBluetoothConnection()
		{
			Dispose( false );
		}

		public Task OpenAsync()
		{
			return Task.Run( () =>
				{
					_tokenSource = new CancellationTokenSource();
					//_client = new BluetoothClient();
					//Console.WriteLine("Connecting BluetoothClient");
					//_client.Connect( _deviceInfo.DeviceAddress, BluetoothService.SerialPort );
					//Console.WriteLine("Getting network stream");
					//_networkStream = _client.GetStream();
					//Console.WriteLine("Checking for Data");
					Task.Factory.StartNew( CheckForData, _tokenSource.Token, TaskCreationOptions.LongRunning,
						TaskScheduler.Default );
				} );
		}

		public void Close()
		{
			if (_tokenSource != null)
			{
				_tokenSource.Cancel();
			}

			//if ( _client.Connected )
			//{
			//_client.Close();
			//}
		}

		public void Write( byte[] data )
		{
			if ( _networkStream.CanWrite )
			{
				_networkStream.Write( data, 0, data.Length );
			}
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		private void Dispose( bool disposing )
		{
			if ( disposing )
			{
				Close();
			}
		}

		private async void CheckForData()
		{
			try
			{
				while ( true )
				{
					if ( _tokenSource.IsCancellationRequested )
						return;

					if ( _networkStream.CanRead /*&& _networkStream.DataAvailable*/ )
					{
						var buffer = new byte[256];
						var numRead = _networkStream.Read( buffer, 0, buffer.Length );
						Array.Resize( ref buffer, numRead );
						DataReceived( this, new BytesReceivedEventArgs( buffer ) );
					}

					if ( _tokenSource.IsCancellationRequested )
						return;
					await Task.Delay( 10 );
				}
			}
			catch ( Exception ex )
			{
				Debug.WriteLine( ex );
			}
		}
	}
}

