using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    // v0 save-flagged its timers, so V0Content must default Next/Delay when a flag is absent.
    [SerializationGenerator(1)]
    public partial class FlaggedTimerItem : ISerializable
    {
        [SerializableField(0)]
        private int _count;

        private Timer _anchoredTimer;
        private Timer _deadlineTimer;
        private Timer _driftTimer;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
            _count = content.Count ?? 0;

            if (content.AnchoredTimerDelay != TimeSpan.MinValue)
            {
                _anchoredTimer = new Timer { Delay = content.AnchoredTimerDelay };
            }

            if (content.DeadlineTimerDelay != TimeSpan.MinValue)
            {
                _deadlineTimer = new Timer { Delay = content.DeadlineTimerDelay };
            }

            if (content.DriftTimerDelay != TimeSpan.MinValue)
            {
                _driftTimer = new Timer { Delay = content.DriftTimerDelay };
            }
        }
    }
}
