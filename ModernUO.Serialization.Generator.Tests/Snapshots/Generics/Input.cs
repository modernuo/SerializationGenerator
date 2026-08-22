using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class GenericItem<T> : ISerializable where T : struct
    {
        [SerializableField(0)]
        private string _name;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }

    [SerializationGenerator(0)]
    public partial class PairItem<TKey, TValue> : ISerializable
        where TKey : class
        where TValue : struct
    {
        [SerializableField(0)]
        private string _label;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
