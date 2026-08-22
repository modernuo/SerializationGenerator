using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class BaseItem : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }

    [SerializationGenerator(0)]
    public partial class DerivedItem : BaseItem
    {
        [SerializableField(0)]
        private int _bonus;
    }
}
