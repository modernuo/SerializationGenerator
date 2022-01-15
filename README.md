# ModernUO Serialization Source Generator

The ModernUO serialization source generator takes the boilerplate out of writing serialization for your classes.
While it is not the most elegant solution (recommendations and contributions are welcome!), it should handle most use-cases.

### How to install
Add `ModernUO.SerializationGenerator` as an analyzer project reference:
```xml
    <ItemGroup>
        <PackageReference Include="ModernUO.SerializationGenerator" Version="1.0.0">
            <SetTargetFramework>TargetFramework=netstandard2.0</SetTargetFramework>
            <OutputItemType>Analyzer</OutputItemType>
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
    </ItemGroup>
```

## Usage


### Basic Usage
All classes will at the very least have this kind of boilerplate:
```cs
    public class ElegantLowTable : Item
    {
        [Constructible]
        public ElegantLowTable() : base(0x2819) => Weight = 1.0;

        public ElegantLowTable(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
          writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
          int version = reader.ReadEncodedInt();
        }
    }
```

The source generator takes some of the tediousness out of writing content by abstracting out the code:
```cs
    [Serializable(0)]
    public partial class ElegantLowTable : Item
    {
        [Constructible]
        public ElegantLowTable() : base(0x2819) => Weight = 1.0;
    }
```

In this example the source generator is aware that the parent class, `Server.Item`, is a `Server.ISerializable`.
By adding the `Serializable` attribute to the class, we are telling the source generator to write the serialization code for us.

There is an accompanying application, the _Serialization Schema Generator_ that generates a migration file for all classes that are annotated.
ModernUO automatically runs this tool through `publish.cmd`. Having the migration scripts will allow the source generator to have a reference to _older version_
of this object and therefore set up a migration path.

_Note: We added `partial` to the class definition to facilitate the actual code generation process. You must use this._

The generated code looks like this:
```css
namespace Server.Items
{
    public partial class ElegantLowTable
    {
        private const int _version = 0;

        public ElegantLowTable(Server.Serial serial) : base(serial)
        {
        }

        public override void Serialize(Server.IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(_version);
        }

        public override void Deserialize(Server.IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
```

### Basic Property Serialization

In our previous example, let's say we wanted to add a property that needs to be serialized. This is how it would look:
```cs
    // Bumped the version from 0 to 1 since we changing
    // the serialization schema by adding a property.
    [Serializable(1)]
    public partial class ElegantLowTable : Item
    {
        [SerializableField(0)]
        private string _prefix;

        [Constructible]
        public ElegantLowTable() : base(0x2819) => Weight = 1.0;

        // Faciliates migrating a v0 object to v1
        private void MigrateFrom(V0Content content)
        {
        }
    }
```

In this example we use the `SerializableField` attribute. This field tells the source generator to create a getter/setter and serialize the property.
By default the getter/setter is public, but that can be modified in the parameters. Since we are modifying the previous example and effectively creating
a new version of this object, we have to bump the `Serializable` field.

Anytime the version is bumped, the project will not compile unless a `MigrateFrom` method is added by the developer to handle migrating from a previous version to the new version.
In this case, the `V0Content` struct contains any/all serialized properties from v0 of the object. The source generator knows the schema of the previous version by reading the v0
migration file generated by the _Serialization Schema Generator_.

_*WARNING*_: DO NOT ASSIGN A VALUE BACK TO THE ORIGINAL PROPERTY (`_prefix`). Always use the generated setter! There are more advanced techniques to handle edge cases if needed.
Contact us in Discord and ask for help if needed!

### Converting Legacy Code & Post deserialization

Quite often we will have code that was not source generated, but we would like to convert it. Sometimes we can cleanly do this without having to modify the version number since
the serialization data would be exactly the same. Other times it isn't clean, or we aren't really sure. To handle this we have to do a conversion, for example:

Legacy:
```css
    public class DeathRobe : Robe
    {
        private DateTime m_DecayTime;
        private Timer m_DecayTimer;

        [Constructible]
        public DeathRobe()
        {
            BeginDecay(m_DefaultDecayTime);
        }

        public DeathRobe(Serial serial) : base(serial)
        {
        }

        // ... Some code

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version

            writer.Write(m_DecayTimer != null);

            if (m_DecayTimer != null)
            {
                writer.WriteDeltaTime(m_DecayTime);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        if (reader.ReadBool())
                        {
                            m_DecayTime = reader.ReadDeltaTime();
                            BeginDecay(m_DecayTime - Core.Now);
                        }

                        break;
                    }
                case 1:
                case 0:
                    {
                        if (Parent == null)
                        {
                            BeginDecay(m_DefaultDecayTime);
                        }

                        break;
                    }
            }

            if (version < 1 && Hue == 0)
            {
                Hue = 2301;
            }
        }
```

The legacy serialization has several versions, it isn't really clean, it has a timer, and does some logic after the deserialization itself.
When the source generator runs, it will see that the version is higher than 0 and then check for migration files. If none exist, it will
require a fallback deserialization method with the signature `private void Deserialize(IGenericReader reader, int version)`.
Here is a complete example of how we would convert this:

```cs
    // We bumped to version 3, and we used the `false` flag to indicate that when we serialize the version property, it is not encoded.
    [Serializable(3, false)]
    public partial class DeathRobe : Robe
    {
        [TimerDrift]
        [SerializableField(0)]
        private Timer _decayTimer;

        // Since the field is a timer, we need to tell the source generator how to convert from a time span to an actual timer.
        // This is a void instead of returning a Timer for flexiblity.
        [DeserializeTimerField(0)]
        private void DeserializeDecayTimer(TimeSpan delay)
        {
            if (delay != TimeSpan.MinValue)
            {
                BeginDecay(delay);
            }
        }

        [Constructible]
        public DeathRobe()
        {
            BeginDecay(m_DefaultDecayTime);
        }

        // ... Some code

        // Executed for versions that are older than our current version, and do not have a migration file (MigrateFrom method).
        private void Deserialize(IGenericReader reader, int version)
        {
            switch (version)
            {
                case 2:
                    {
                        if (reader.ReadBool())
                        {
                            m_DecayTime = reader.ReadDeltaTime();
                            BeginDecay(m_DecayTime - Core.Now);
                        }

                        break;
                    }
                case 1:
                case 0:
                    {
                        if (Parent == null)
                        {
                            BeginDecay(m_DefaultDecayTime);
                        }

                        break;
                    }
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (version < 1 && Hue == 0)
            {
                Hue = 2301;
            }
        }
    }
```
