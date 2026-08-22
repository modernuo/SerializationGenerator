using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class BasicFieldsItem : ISerializable
    {
        [SerializableField(0)]
        private int _intValue;

        [SerializableField(1)]
        private string _name;

        [SerializableField(2)]
        [InternString]
        private string _interned;

        [SerializableField(3)]
        private bool _active;

        [SerializableField(4)]
        private double _weight;

        [SerializableField(5)]
        private DateTime _crafted;

        [SerializableField(6)]
        [DeltaDateTime]
        private DateTime _lastUsed;

        [SerializableField(7)]
        private TimeSpan _duration;

        [SerializableField(8)]
        private Guid _identifier;

        [SerializableField(9)]
        [EncodedInt]
        private int _encoded;

        [SerializableField(10)]
        private decimal _price;

        [SerializableField(11)]
        private byte _small;

        [SerializableField(12)]
        private sbyte _signedSmall;

        [SerializableField(13)]
        private short _shortValue;

        [SerializableField(14)]
        private ushort _unsignedShort;

        [SerializableField(15)]
        private uint _unsignedInt;

        [SerializableField(16)]
        private long _longValue;

        [SerializableField(17)]
        private ulong _unsignedLong;

        [SerializableField(18)]
        private float _floatValue;

        [SerializableField(19, setter: "private")]
        private int _privateSet;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
