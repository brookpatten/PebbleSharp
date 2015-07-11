using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PebbleSharp.Core.NonPortable.AppMessage
{
    //modeled after https://github.com/pebble/libpebble2/blob/master/libpebble2/services/appmessage.py

    public class AppMessageDictionary
    {
        //byte layout is as follows
        //command (always 1?) byte
        //transactionid       byte
        //uuid                byte[16]
        //tuplecount          byte
        //tuple               
        //  k                 uint      key
        //  t                 byte      type 
        //  l                 ushort    length


        public const byte COMMAND = 1;

        public AppMessageDictionary()
        {
            Values = new List<IAppMessageDictionaryEntry>();
        }

        public IList<IAppMessageDictionaryEntry> Values { get; set; }

        public byte[] GetBytes(byte transactionId,UUID appId)
        {
            if (Values != null && Values.Any())
            {
                var bytes = new List<byte>();
                bytes.Add(COMMAND);
                bytes.Add(transactionId);
                bytes.AddRange(appId.Data);
                bytes.Add((byte)Values.Count);
                uint index = 0;
                foreach (var tuple in Values)
                {
                    tuple.Key = index;
                    bytes.AddRange(tuple.PackedBytes);
                    index++;
                }
                return bytes.ToArray();
            }
            else
            {
                return new byte[0];
            }
        }
    }

    public interface IAppMessageDictionaryEntry
    {
        uint Key { get; set; }
        PackedType PackedType { get; }
        ushort Length { get; }
        byte[] ValueBytes { get; set; }
        byte[] PackedBytes { get; }
    }

    public enum PackedType:byte
    {
        Bytes=0,
        String =1,
        Unsigned =2,
        Signed = 3
    }

    public abstract class AppMessageDictionaryEntry<T> : IAppMessageDictionaryEntry
    {
        public uint Key { get; set; }
        public abstract PackedType PackedType { get; }
        public abstract ushort Length { get; }

        public T Value { get; set; }
        public abstract byte[] ValueBytes { get; set; }

        public byte[] PackedBytes
        {
            get
            {
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(Key));
                bytes.Add((byte)PackedType);
                bytes.AddRange(BitConverter.GetBytes(Length));
                bytes.AddRange(ValueBytes);
                return bytes.ToArray();
            }
        }
    }

    public class AppMessageUInt8 : AppMessageDictionaryEntry<byte>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Unsigned; }
        }

        public override ushort Length
        {
            get { return sizeof(byte); }
        }

        public override byte[] ValueBytes
        {
            get { return new byte[] {Value}; }
            set
            {
                if (value.Length == Length)
                {
                    Value = value[0];
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageUInt16 : AppMessageDictionaryEntry<UInt16>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Unsigned; }
        }

        public override ushort Length
        {
            get { return sizeof(UInt16); }
        }

        public override byte[] ValueBytes
        {
            get { return BitConverter.GetBytes(Value); }
            set
            {
                if (value.Length == Length)
                {
                    Value = BitConverter.ToUInt16(value,0);
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageUInt32 : AppMessageDictionaryEntry<UInt32>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Unsigned; }
        }

        public override ushort Length
        {
            get { return sizeof(UInt32); }
        }

        public override byte[] ValueBytes
        {
            get { return BitConverter.GetBytes(Value); }
            set
            {
                if (value.Length == Length)
                {
                    Value = BitConverter.ToUInt32(value, 0);
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageInt8 : AppMessageDictionaryEntry<sbyte>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Signed; }
        }

        public override ushort Length
        {
            get { return sizeof(sbyte); }
        }

        public override byte[] ValueBytes
        {
            get { return new byte[] { Convert.ToByte(Value) }; }
            set
            {
                if (value.Length == Length)
                {
                    Value = Convert.ToSByte(value);
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageInt16 : AppMessageDictionaryEntry<Int16>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Signed; }
        }

        public override ushort Length
        {
            get { return sizeof(Int16); }
        }

        public override byte[] ValueBytes
        {
            get { return BitConverter.GetBytes(Value); }
            set
            {
                if (value.Length == Length)
                {
                    Value = BitConverter.ToInt16(value, 0);
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageInt32 : AppMessageDictionaryEntry<Int32>
    {
        public override PackedType PackedType
        {
            get { return PackedType.Signed; }
        }

        public override ushort Length
        {
            get { return sizeof(Int32); }
        }

        public override byte[] ValueBytes
        {
            get { return BitConverter.GetBytes(Value); }
            set
            {
                if (value.Length == Length)
                {
                    Value = BitConverter.ToInt32(value, 0);
                }
                else
                {
                    throw new InvalidOperationException("Incorrect # of bytes");
                }
            }
        }
    }

    public class AppMessageString : AppMessageDictionaryEntry<string>
    {
        public override PackedType PackedType
        {
            get { return PackedType.String; }
        }

        public override ushort Length
        {
            get { return (ushort)System.Text.UTF8Encoding.UTF8.GetBytes(Value).Length; }
        }

        public override byte[] ValueBytes
        {
            get { return System.Text.UTF8Encoding.UTF8.GetBytes(Value); }
            set
            {
                if (value.Length <= ushort.MaxValue)
                {
                    Value = System.Text.UTF8Encoding.UTF8.GetString(value);
                }
                else
                {
                    throw new OverflowException("Specified string is too large for length to fit in a ushort");
                }
            }
        }
    }

    public class AppMessageBytes : AppMessageDictionaryEntry<byte[]>
    {
        public override PackedType PackedType
        {
            get { return PackedType.String; }
        }

        public override ushort Length
        {
            get { return (ushort)Value.Length; }
        }

        public override byte[] ValueBytes
        {
            get { return Value; }
            set
            {
                if (value.Length <= ushort.MaxValue)
                {
                    Value = value;
                }
                else
                {
                    throw new OverflowException("Specified array is too large for length to fit in a ushort");
                }
            }
        }
    }
}
