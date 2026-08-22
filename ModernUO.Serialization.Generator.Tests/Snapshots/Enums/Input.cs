using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    public enum ItemQuality
    {
        Low,
        Regular,
        Exceptional
    }

    [Flags]
    public enum ItemTraits
    {
        None = 0x0,
        Cursed = 0x1,
        Blessed = 0x2,
        Insured = 0x4
    }

    [SerializationGenerator(0)]
    public partial class EnumsItem : ISerializable
    {
        [SerializableField(0)]
        private ItemQuality _quality;

        [SerializableField(1)]
        private ItemTraits _traits;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
