using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class FieldLinkedItem : ISerializable
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

        [SerializableField(2, fieldChanged: nameof(OnLevelChanged))]
        private int _level;

        private void OnLevelChanged(int oldValue, int newValue)
        {
        }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
