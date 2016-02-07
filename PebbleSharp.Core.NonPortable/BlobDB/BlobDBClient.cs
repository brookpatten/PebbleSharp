using System;
using System.Threading.Tasks;

namespace PebbleSharp.Core.BlobDB
{
	public class BlobDBClient
	{
		private Pebble _pebble;
		private Random _random;

		public BlobDBClient(Pebble pebble)
		{
			_pebble = pebble;
			_random = new Random();
		}
		public async Task<BlobDBResponse> Insert(BlobDatabase database, byte[] key, byte[] value)
		{
			var insertCommand = new BlobDBCommand()
			{
				Token = GenerateToken(),
				Database = database,
				Command = BlobCommand.Insert,
				Key=key,
				Value=value
			};
			return await Send(insertCommand);
		}
		public async Task<BlobDBResponse> Delete(BlobDatabase database, byte[] key)
		{
			var deleteCommand = new BlobDBCommand()
			{
				Token = GenerateToken(),
				Database = database,
				Command = BlobCommand.Delete,
				Key=key,
			};
			return await Send(deleteCommand);
		}
		public async Task<BlobDBResponse> Clear(BlobDatabase database)
		{
			var clearCommand = new BlobDBCommand()
			{
				Token = GenerateToken(),
				Database = database,
				Command = BlobCommand.Clear
			};
			return await Send(clearCommand);
		}
		private async Task<BlobDBResponse> Send(BlobDBCommand command)
		{
			Console.WriteLine("Sending Token " + command.Token);
			return await _pebble.SendBlobDBMessage(command);
		}
		public ushort GenerateToken()
		{
			//this is how libpebble2 does it...random.randrange(1, 2**16 - 1, 1)
			return (ushort)_random.Next(1, (2 ^ 16) - 1);
		}
	}
}

