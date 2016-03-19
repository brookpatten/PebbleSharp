using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PebbleSharp.Core;

namespace PebbleSharp.Mono.BlueZ5
{
    public sealed class Zip : IZip
    {
		private ZipFile _zipFile;
        private Stream _zipStream;

        ~Zip()
        {
            Dispose( false );
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
                if ( _zipStream != null )
                {
                    if ( _zipStream.CanSeek )
                        _zipStream.Seek( 0, SeekOrigin.Begin );
                    _zipStream = null;
                }
                if ( _zipFile != null )
                {
                    _zipFile = null;
                }
            }
        }

        public bool Open( Stream zipStream )
        {
			//this ensures that we load the code page, otherwise on mono it might not be available
			//and will cause an error when the zip lib attempts to open it
			_zipStream = zipStream;
			_zipFile = new ZipFile (zipStream);
            return true;
        }

        public Stream OpenEntryStream( string zipEntryName )
        {
			return new ZipStreamWrapper (_zipFile, zipEntryName);
        }
    }

	public class ZipStreamWrapper : Stream
	{
		private ZipFile _file;
		private ZipEntry _entry;
		private string _entryName;
		private Stream _entryStream;
		public ZipStreamWrapper (ZipFile file, string entryName)
		{
			_file = file;
			_entryName = entryName;
			_entry = _file.GetEntry (_entryName);

			if (_entry != null) {
				_entryStream = _file.GetInputStream (_entry);
			} 
			else 
			{
				throw new ZipException ("entry " + entryName + " not found");
			}
		}

		public override long Position 
		{
			get 
			{
				//TODO: figure out what this 30 bytes is, I'm guessing some kind of header
				return _entryStream.Position - _entry.Offset - 30;
			}
			set 
			{
				//TODO: same as getter
				_entryStream.Position = _entry.Offset + value + 30;
			}
		}

		public override long Length {
			get 
			{
				return _entry.Size;
			}
		}

		public override bool CanRead {
			get { return _entryStream.CanRead;}
				
		}

		public override bool CanSeek {
			get { return _entryStream.CanSeek;}
		}

		public override bool CanTimeout {
			get { return _entryStream.CanTimeout; }
		}

		public override bool CanWrite {
			get { return _entryStream.CanWrite;}
		}


		public override int ReadTimeout {
			get {return _entryStream.ReadTimeout;}
			set { _entryStream.ReadTimeout = value; }
		}

		public override int WriteTimeout {
			get {return _entryStream.WriteTimeout;}
			set { _entryStream.WriteTimeout = value; }
		}

		public override IAsyncResult BeginRead (byte [] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return _entryStream.BeginRead (buffer, offset + _entry.Offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite (byte [] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return _entryStream.BeginWrite(buffer,offset+_entry.Offset,count,callback,state);
		}

		public override void Close ()
		{
			_entryStream.Close ();
		}

		public override Task CopyToAsync (Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return _entryStream.CopyToAsync (destination, bufferSize,cancellationToken);
		}

		public void Dispose ()
		{
			_entryStream.Dispose ();
		}

		protected override void Dispose (bool disposing)
		{
			_entryStream.Dispose ();
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			return _entryStream.EndRead (asyncResult);
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			_entryStream.EndWrite (asyncResult);
		}

		public override void Flush ()
		{
			_entryStream.Flush ();
		}

		public override Task FlushAsync (CancellationToken cancellationToken)
		{
			return _entryStream.FlushAsync (cancellationToken);
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			return _entryStream.Read (buffer, offset, count);
		}

		public override Task<int> ReadAsync (byte [] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _entryStream.ReadAsync (buffer, offset, count, cancellationToken);
		}

		public override int ReadByte ()
		{
			return _entryStream.ReadByte ();
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			return _entryStream.Seek (_entry.Offset+offset, origin);
		}

		public override void SetLength (long value)
		{
			_entryStream.SetLength (value);
		}

		public override void Write (byte [] buffer, int offset, int count)
		{
			_entryStream.Write (buffer, _entry.Offset+offset, count);
		}

		public override Task WriteAsync (byte [] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _entryStream.WriteAsync (buffer, offset, count, cancellationToken);
		}

		public override void WriteByte (byte value)
		{
			_entryStream.WriteByte (value);
		}
	}
}