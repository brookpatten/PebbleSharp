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
				header = Util.CombineArrays(new byte[] { ((byte)PutBytesType.Init) }, length, new[] { hackedTransferType},BitConverter.GetBytes(appInstallId));
			}
			else 
			{
				throw new ArgumentException("Must specifiy either index or appInstallId");
			}

			var rawMessageArgs = await _pebble.SendMessageAsync<PutBytesResponse>( Endpoint.PutBytes, header );
			if (rawMessageArgs.Success == false)
			{
				return false;
			}
			else
			{
				System.Console.WriteLine("Init:OK");
			}

			byte[] tokenResult = rawMessageArgs.Response;
			byte[] token = tokenResult.Skip( 1 ).ToArray();

			if (token.Length != 4)
			{
				throw new Exception("Bad token");
			}

			uint tokenInt = BitConverter.ToUInt32(token, 0);
			System.Console.WriteLine("Token: " + tokenInt);

			const int BUFFER_SIZE = 2000;
			//Send at most 2000 bytes at a time
			for ( int i = 0; i <= binary.Length / BUFFER_SIZE; i++ )
			{
				byte[] data = binary.Skip( BUFFER_SIZE * i ).Take( BUFFER_SIZE ).ToArray();
				byte[] dataHeader = Util.CombineArrays( new byte[] { (byte)PutBytesType.Put }, token, Util.GetBytes( data.Length ) );
				var result = await _pebble.SendMessageAsync<PutBytesResponse>( Endpoint.PutBytes, Util.CombineArrays( dataHeader, data ) );
				if ( result.Success == false )
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
			byte[] commitMessage = Util.CombineArrays( new byte[] { (byte)PutBytesType.Commit }, token, crcBytes );
			var commitResult = await _pebble.SendMessageAsync<PutBytesResponse>( Endpoint.PutBytes, commitMessage );
			if ( commitResult.Success == false )
			{
				await AbortPutBytesAsync( token );
				return false;
			}
			else
			{
				System.Console.WriteLine("Commit:OK");
			}


			//Send install message
			byte[] completeMessage = Util.CombineArrays( new byte[] { (byte)PutBytesType.Install }, token );
			var completeResult = await _pebble.SendMessageAsync<PutBytesResponse>( Endpoint.PutBytes, completeMessage );
			if (completeResult.Success == false)
			{
				await AbortPutBytesAsync(token);
			}
			else
			{
				System.Console.WriteLine("Install:OK");
			}
			return completeResult.Success;
		}


		private async Task<PutBytesResponse> AbortPutBytesAsync( byte[] token )
		{
			if ( token == null ) throw new ArgumentNullException( "token" );

			byte[] data = Util.CombineArrays( new byte[] { (byte)PutBytesType.Abort }, token );

			return await _pebble.SendMessageAsync<PutBytesResponse>( Endpoint.PutBytes, data );
		}
	}
}

