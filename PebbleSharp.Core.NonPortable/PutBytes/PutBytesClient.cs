using System;
using System.Threading.Tasks;
using System.Linq;
using PebbleSharp.Core.Responses;
namespace PebbleSharp.Core
{
	public class PutBytesClient
	{
		private Pebble _pebble;

		public PutBytesClient(Pebble pebble)
		{
			_pebble = pebble;
		}

		public async Task<bool> PutBytes( byte[] binary, TransferType transferType,byte index=byte.MaxValue,uint appInstallId=uint.MinValue)
		{
			System.Console.WriteLine("Putting " + binary.Length + " bytes");
			byte[] length = Util.GetBytes( binary.Length );

			//Get token
			byte[] header;

			if (index != byte.MaxValue)
			{
				System.Console.WriteLine("index: " + index);
				header = Util.CombineArrays(new byte[] { (byte)PutBytesType.Init }, length, new[] { (byte)transferType, index });
			}
			else if (appInstallId != uint.MinValue)
			{
				System.Console.WriteLine("Installing AppId: " + appInstallId);
				byte hackedTransferType = (byte)transferType;
				hackedTransferType |= (1 << 7); //just put that bit anywhere...
				header = Util.CombineArrays(new byte[] { ((byte)PutBytesType.Init) }, length, new[] { hackedTransferType},Util.GetBytes(appInstallId));
			}
			else 
			{
				throw new ArgumentException("Must specifiy either index or appInstallId");
			}

			var rawMessageArgs = await _pebble.SendMessageAsync<PutBytesResponsePacket>( Endpoint.PutBytes, header );
			if (rawMessageArgs.Result == PutBytesResult.Nack)
			{
				return false;
			}
			else
			{
				System.Console.WriteLine("Init:OK");
			}

			var token = rawMessageArgs.Token;
			System.Console.WriteLine("Token: " + token);

			const int BUFFER_SIZE = 2000;
			//Send at most 2000 bytes at a time
			for ( int i = 0; i <= binary.Length / BUFFER_SIZE; i++ )
			{
				byte[] data = binary.Skip( BUFFER_SIZE * i ).Take( BUFFER_SIZE ).ToArray();
				byte[] dataHeader = Util.CombineArrays( new byte[] { (byte)PutBytesType.Put }, Util.GetBytes(token), Util.GetBytes( data.Length ) );
				var result = await _pebble.SendMessageAsync<PutBytesResponsePacket>( Endpoint.PutBytes, Util.CombineArrays( dataHeader, data ) );
				if ( result.Result == PutBytesResult.Nack )
				{
					await AbortPutBytesAsync( token );
					return false;
				}
				else
				{
					System.Console.WriteLine("Put:OK");
				}
			}

			//Send commit message
			uint crc = Crc32.Calculate( binary );
			byte[] crcBytes = Util.GetBytes( crc );
			System.Console.WriteLine("CRC:" + crc);
			byte[] commitMessage = Util.CombineArrays( new byte[] { (byte)PutBytesType.Commit }, Util.GetBytes(token), crcBytes );
			var commitResult = await _pebble.SendMessageAsync<PutBytesResponsePacket>( Endpoint.PutBytes, commitMessage );
			if ( commitResult.Result == PutBytesResult.Nack )
			{
				await AbortPutBytesAsync( token );
				return false;
			}
			else
			{
				System.Console.WriteLine("Commit:OK");
			}


			//Send install message
			byte[] completeMessage = Util.CombineArrays( new byte[] { (byte)PutBytesType.Install }, Util.GetBytes(token) );
			var completeResult = await _pebble.SendMessageAsync<PutBytesResponsePacket>( Endpoint.PutBytes, completeMessage );
			if (completeResult.Result == PutBytesResult.Nack)
			{
				await AbortPutBytesAsync(token);
			}
			else
			{
				System.Console.WriteLine("Install:OK");
			}
			return completeResult.Success;
		}


		private async Task<PutBytesResponsePacket> AbortPutBytesAsync( uint token )
		{
			byte[] data = Util.CombineArrays( new byte[] { (byte)PutBytesType.Abort }, Util.GetBytes(token) );

			return await _pebble.SendMessageAsync<PutBytesResponsePacket>( Endpoint.PutBytes, data );
		}
	}
}

