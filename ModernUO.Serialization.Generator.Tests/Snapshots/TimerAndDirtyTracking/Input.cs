using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class OwnerEntity : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }

    [SerializationGenerator(0, false)]
    public partial class TrackedChild
    {
        [DirtyTrackingEntity]
        private OwnerEntity _owner;

        [SerializableField(0)]
        [DeserializeTimer(nameof(DeserializeRefreshTimer))]
        private Timer _refreshTimer;

        private void DeserializeRefreshTimer(TimeSpan delay)
        {
            _refreshTimer = new Timer();
        }

        [SerializableField(1)]
        private int _progress;
    }
}
