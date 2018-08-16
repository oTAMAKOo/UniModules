﻿﻿
using UnityEngine;
using System;
using System.Runtime.Serialization;

namespace Extensions.Serialize
{
    [DataContract]
    [Serializable]
    public abstract class SerializableNullable<T>
    {
        //----- params -----

        //----- field -----

        [DataMember]
        [SerializeField]
        protected bool hasValue = false;

        [DataMember]
        [SerializeField]
        protected T value = default(T);

        //----- property -----

        [IgnoreDataMember]
        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException();
                }

                return value;
            }

            set
            {
                this.value = value;
                hasValue = true;
            }
        }

        [IgnoreDataMember]
        public bool HasValue
        {
            get { return hasValue; }
        }

        //----- method -----

        public SerializableNullable(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public SerializableNullable()
        {
            this.hasValue = false;
        }

        public T GetValueOrDefault()
        {
            return value;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            return HasValue ? value : defaultValue;
        }

        public static explicit operator T(SerializableNullable<T> TValue)
        {
            return TValue.Value;
        }

        public override string ToString()
        {
            return HasValue ? value.ToString() : string.Empty;
        }
    }
}