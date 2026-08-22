using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class SaveFlagsItem : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        [SerializableFieldSaveFlag(0)]
        private bool ShouldSerializeName() => _name != null;

        [SerializableField(1)]
        private int _charges;

        [SerializableFieldSaveFlag(1)]
        private bool ShouldSerializeCharges() => _charges != 8;

        [SerializableFieldDefault(1)]
        private int ChargesDefaultValue() => 8;

        [SerializableField(2)]
        private DateTime _expires;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializeExpires() => _expires != DateTime.MinValue;

        [SerializableFieldDefault(2)]
        private DateTime ExpiresDefaultValue() => DateTime.MinValue;

        [SerializableField(3)]
        private bool _identified;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
