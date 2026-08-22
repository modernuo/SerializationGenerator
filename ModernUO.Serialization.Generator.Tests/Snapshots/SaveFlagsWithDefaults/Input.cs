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

        [SerializableFieldSaveFlag(nameof(_name))]
        private bool ShouldSerializeName() => _name != null;

        [SerializableField(1)]
        private int _charges;

        [SerializableFieldSaveFlag(nameof(_charges))]
        private bool ShouldSerializeCharges() => _charges != 8;

        [SerializableFieldDefault(nameof(_charges))]
        private int ChargesDefaultValue() => 8;

        [SerializableField(2)]
        private DateTime _expires;

        [SerializableFieldSaveFlag(nameof(_expires))]
        private bool ShouldSerializeExpires() => _expires != DateTime.MinValue;

        [SerializableFieldDefault(nameof(_expires))]
        private DateTime ExpiresDefaultValue() => DateTime.MinValue;

        [SerializableField(3)]
        private bool _identified;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
