using System;
using ModernUO.Serialization;
using Server;

namespace TestContent
{
    [SerializationGenerator(1)]
    public partial class ExternalItem : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        [SerializableField(1)]
        [DeserializeTimer(nameof(DeserializeRefreshTimer))]
        private Timer _refreshTimer;

        private void DeserializeRefreshTimer(TimeSpan delay)
        {
            _refreshTimer = new Timer();
        }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
            _name = content.Name;
        }

        [AfterDeserialization]
        private void ValidateState()
        {
        }

        [AfterDeserialization(false)]
        private void RebuildCaches()
        {
        }
    }
}
