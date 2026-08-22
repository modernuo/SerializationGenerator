using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class KeyValuePairItem : ISerializable
    {
        [SerializableField(0)]
        private KeyValuePair<int, string> _selected;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
