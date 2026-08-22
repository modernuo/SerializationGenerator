using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(1)]
    public partial class TimersItem : ISerializable
    {
        [SerializableField(0)]
        [DeserializeTimer(nameof(RestartDriftTimer))]
        private Timer _driftTimer;

        private void RestartDriftTimer(TimeSpan delay)
        {
            _driftTimer = new Timer();
        }

        [SerializableField(1)]
        [DeserializeTimer(nameof(RestartDeadlineTimer), wallClock: true)]
        private Timer _deadlineTimer;

        private void RestartDeadlineTimer(TimeSpan delay)
        {
            _deadlineTimer = new Timer();
        }

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
        }
    }
}
