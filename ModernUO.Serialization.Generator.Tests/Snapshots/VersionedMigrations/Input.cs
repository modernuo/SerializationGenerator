using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(2)]
    public partial class MigratingItem : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        [SerializableField(1)]
        private int _charges;

        [SerializableField(2)]
        private bool _identified;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
            _name = content.Name;
            _charges = 0;
            _identified = false;
        }

        private void MigrateFrom(V1Content content)
        {
            _name = content.Name;
            _charges = content.Charges;
            _identified = false;
        }
    }
}
