
using System;
using System.Collections.Generic;

namespace Extensions
{
    [Serializable]
    public sealed class SecureValue<T>
    {
        //----- params -----

        private static readonly Dictionary<Type, Type> SecureValueTypeTable = new Dictionary<Type, Type>
        {
            { typeof(bool),     typeof(BoolSecureValue)     },

            { typeof(sbyte),    typeof(SByteSecureValue)    },
            { typeof(byte),     typeof(ByteSecureValue)     },

            { typeof(short),    typeof(ShortSecureValue)    },
            { typeof(ushort),   typeof(UShortSecureValue)   },

            { typeof(int),      typeof(IntSecureValue)      },
            { typeof(uint),     typeof(UIntSecureValue)     },

            { typeof(long),     typeof(LongSecureValue)     },
            { typeof(ulong),    typeof(ULongSecureValue)    },

            { typeof(float),    typeof(FloatSecureValue)    },
            { typeof(double),   typeof(DoubleSecureValue)   },
        };

        private abstract class SecureData<TValue>
        {
            //----- params -----

            private readonly byte[] bytes = null;

            private readonly byte seed = 0;

            //----- field -----

            private byte[] buffer = null;

            //----- property -----

            public TValue Value
            {
                get
                {
                    bytes.CopyTo(buffer, 0);

                    Xor();

                    return ConvertValue(buffer);
                }

                set
                {
                    buffer = GetValueBytes(value);

                    Xor();

                    buffer.CopyTo(bytes, 0);
                }
            }
            
            //----- method -----

            public SecureData(int bufferSize)
            {
                buffer = new byte[bufferSize];
                bytes = new byte[bufferSize];

                var random = new Random();

                seed = (byte)(random.Next() << 32 | random.Next());

                Value = default(TValue);
            }

            private void Xor()
            {
                for (var i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] ^= seed;
                }
            }

            protected abstract byte[] GetValueBytes(TValue value);

            protected abstract TValue ConvertValue(byte[] buffer);
        }

        //----- field -----

        private SecureData<T> secureData = null;

        //----- property -----

        public T Value
        {
            get { return secureData.Value; }

            set { secureData.Value = value; }
        }

        //----- method -----

        public SecureValue()
        {
            var type = SecureValueTypeTable.GetValueOrDefault(typeof(T));

            if (type == null)
            {
                throw new NotSupportedException(string.Format("Type {0} is not support.", typeof(T)));
            }

            secureData = (SecureData<T>)Activator.CreateInstance(type);
        }

        public SecureValue(T value) : this()
        {
            secureData.Value = value;
        }

        #region bool

        [Serializable]
        private sealed class BoolSecureValue : SecureData<bool>
        {
            public BoolSecureValue() : base(sizeof(bool)){}

            protected override byte[] GetValueBytes(bool value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override bool ConvertValue(byte[] buffer)
            {
                return BitConverter.ToBoolean(buffer, 0);
            }
        }

        #endregion

        #region sbyte, byte

        [Serializable]
        private sealed class SByteSecureValue : SecureData<sbyte>
        {
            public SByteSecureValue() : base(sizeof(sbyte)) { }

            protected override byte[] GetValueBytes(sbyte value)
            {
                return new byte[]{ (byte)value };
            }

            protected override sbyte ConvertValue(byte[] buffer)
            {
                return (sbyte)buffer[0];
            }
        }
        
        [Serializable]
        private sealed class ByteSecureValue : SecureData<byte>
        {
            public ByteSecureValue() : base(sizeof(byte)) { }

            protected override byte[] GetValueBytes(byte value)
            {
                return new byte[] { (byte)value };
            }

            protected override byte ConvertValue(byte[] buffer)
            {
                return (byte)buffer[0];
            }
        }

        #endregion

        #region short, ushort

        [Serializable]
        private sealed class ShortSecureValue : SecureData<short>
        {
            public ShortSecureValue() : base(sizeof(short)) { }

            protected override byte[] GetValueBytes(short value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override short ConvertValue(byte[] buffer)
            {
                return BitConverter.ToInt16(buffer, 0);
            }
        }

        [Serializable]
        private sealed class UShortSecureValue : SecureData<ushort>
        {
            public UShortSecureValue() : base(sizeof(ushort)) { }

            protected override byte[] GetValueBytes(ushort value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override ushort ConvertValue(byte[] buffer)
            {
                return BitConverter.ToUInt16(buffer, 0);
            }
        }

        #endregion

        #region int, uint

        [Serializable]
        private sealed class IntSecureValue : SecureData<int>
        {
            public IntSecureValue() : base(sizeof(int)) { }

            protected override byte[] GetValueBytes(int value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override int ConvertValue(byte[] buffer)
            {
                return BitConverter.ToInt32(buffer, 0);
            }
        }

        [Serializable]
        private sealed class UIntSecureValue : SecureData<uint>
        {
            public UIntSecureValue() : base(sizeof(uint)) { }

            protected override byte[] GetValueBytes(uint value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override uint ConvertValue(byte[] buffer)
            {
                return BitConverter.ToUInt32(buffer, 0);
            }
        }

        #endregion

        #region long, ulong

        [Serializable]
        private sealed class LongSecureValue : SecureData<long>
        {
            public LongSecureValue() : base(sizeof(long)) { }

            protected override byte[] GetValueBytes(long value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override long ConvertValue(byte[] buffer)
            {
                return BitConverter.ToInt64(buffer, 0);
            }
        }

        [Serializable]
        private sealed class ULongSecureValue : SecureData<ulong>
        {
            public ULongSecureValue() : base(sizeof(ulong)) { }

            protected override byte[] GetValueBytes(ulong value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override ulong ConvertValue(byte[] buffer)
            {
                return BitConverter.ToUInt64(buffer, 0);
            }
        }

        #endregion

        #region float, double

        [Serializable]
        private sealed class FloatSecureValue : SecureData<float>
        {
            public FloatSecureValue() : base(sizeof(float)) { }

            protected override byte[] GetValueBytes(float value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override float ConvertValue(byte[] buffer)
            {
                return BitConverter.ToSingle(buffer, 0);
            }
        }

        [Serializable]
        private sealed class DoubleSecureValue : SecureData<double>
        {
            public DoubleSecureValue() : base(sizeof(double)) { }

            protected override byte[] GetValueBytes(double value)
            {
                return BitConverter.GetBytes(value);
            }

            protected override double ConvertValue(byte[] buffer)
            {
                return BitConverter.ToDouble(buffer, 0);
            }
        }

        #endregion
    }
}
