using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class SaveFlagsItem : ISerializable
    {
        [SerializableField(0)]
        [SaveFlag(nameof(ShouldSerializeName))]
        private string _name;

        private bool ShouldSerializeName() => _name != null;

        [SerializableField(1)]
        [SaveFlag(nameof(ShouldSerializeCharges), nameof(ChargesDefaultValue))]
        private int _charges;

        private bool ShouldSerializeCharges() => _charges != 8;

        private int ChargesDefaultValue() => 8;

        [SerializableField(2)]
        [SaveFlag(nameof(ShouldSerializeExpires), nameof(ExpiresDefaultValue))]
        private DateTime _expires;

        private bool ShouldSerializeExpires() => _expires != DateTime.MinValue;

        private DateTime ExpiresDefaultValue() => DateTime.MinValue;

        [SerializableField(3)]
        private bool _identified;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
