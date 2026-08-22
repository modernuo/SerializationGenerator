using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class ReadonlyItem : ISerializable
    {
        [SerializableField(0)]
        private readonly string _id;

        [SerializableField(1)]
        private string _name;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
