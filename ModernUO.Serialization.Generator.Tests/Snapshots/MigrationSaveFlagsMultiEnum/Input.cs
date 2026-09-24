using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    // v0 had 66 save-flagged fields: a ulong V0SaveFlag holding 64 and a V0SaveFlag2 holding the rest.
    [SerializationGenerator(1)]
    public partial class WideMigratingItem : ISerializable
    {
        [SerializableField(0)]
        private string _name;

        [SerializableField(1)]
        private int _total;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }

        private void MigrateFrom(V0Content content)
        {
            _name = content.Name;
            _total += content.Field0 ?? 0;
            _total += content.Field1 ?? 0;
            _total += content.Field2 ?? 0;
            _total += content.Field3 ?? 0;
            _total += content.Field4 ?? 0;
            _total += content.Field5 ?? 0;
            _total += content.Field6 ?? 0;
            _total += content.Field7 ?? 0;
            _total += content.Field8 ?? 0;
            _total += content.Field9 ?? 0;
            _total += content.Field10 ?? 0;
            _total += content.Field11 ?? 0;
            _total += content.Field12 ?? 0;
            _total += content.Field13 ?? 0;
            _total += content.Field14 ?? 0;
            _total += content.Field15 ?? 0;
            _total += content.Field16 ?? 0;
            _total += content.Field17 ?? 0;
            _total += content.Field18 ?? 0;
            _total += content.Field19 ?? 0;
            _total += content.Field20 ?? 0;
            _total += content.Field21 ?? 0;
            _total += content.Field22 ?? 0;
            _total += content.Field23 ?? 0;
            _total += content.Field24 ?? 0;
            _total += content.Field25 ?? 0;
            _total += content.Field26 ?? 0;
            _total += content.Field27 ?? 0;
            _total += content.Field28 ?? 0;
            _total += content.Field29 ?? 0;
            _total += content.Field30 ?? 0;
            _total += content.Field31 ?? 0;
            _total += content.Field32 ?? 0;
            _total += content.Field33 ?? 0;
            _total += content.Field34 ?? 0;
            _total += content.Field35 ?? 0;
            _total += content.Field36 ?? 0;
            _total += content.Field37 ?? 0;
            _total += content.Field38 ?? 0;
            _total += content.Field39 ?? 0;
            _total += content.Field40 ?? 0;
            _total += content.Field41 ?? 0;
            _total += content.Field42 ?? 0;
            _total += content.Field43 ?? 0;
            _total += content.Field44 ?? 0;
            _total += content.Field45 ?? 0;
            _total += content.Field46 ?? 0;
            _total += content.Field47 ?? 0;
            _total += content.Field48 ?? 0;
            _total += content.Field49 ?? 0;
            _total += content.Field50 ?? 0;
            _total += content.Field51 ?? 0;
            _total += content.Field52 ?? 0;
            _total += content.Field53 ?? 0;
            _total += content.Field54 ?? 0;
            _total += content.Field55 ?? 0;
            _total += content.Field56 ?? 0;
            _total += content.Field57 ?? 0;
            _total += content.Field58 ?? 0;
            _total += content.Field59 ?? 0;
            _total += content.Field60 ?? 0;
            _total += content.Field61 ?? 0;
            _total += content.Field62 ?? 0;
            _total += content.Field63 ?? 0;
            _total += content.Field64 ?? 0;
            _total += content.Field65 ?? 0;
        }
    }
}
