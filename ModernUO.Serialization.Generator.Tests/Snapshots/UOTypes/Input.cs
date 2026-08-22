using System;
using ModernUO.Serialization;
using Server;

namespace Server.TestContent
{
    [SerializationGenerator(0)]
    public partial class UOTypesItem : ISerializable
    {
        [SerializableField(0)]
        private Serial _linkedSerial;

        [SerializableField(1)]
        private Point2D _location2D;

        [SerializableField(2)]
        private Point3D _location3D;

        [SerializableField(3)]
        private Rectangle2D _bounds2D;

        [SerializableField(4)]
        private Rectangle3D _bounds3D;

        public DateTime Created { get; set; }
        public Serial Serial { get; }
        public bool Deleted => false;
        public void Delete() { }
    }
}
