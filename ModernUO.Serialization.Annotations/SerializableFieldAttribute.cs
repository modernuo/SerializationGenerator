/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldAttribute.cs                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace ModernUO.Serialization;

/// <summary>
/// Hints to the source generator that this field should be serialized.
/// The source generator will generate the property entirely.
/// <para>
/// <c>allowFieldChange</c> names a gate with the signature <c>bool Method(ref T value)</c>
/// where T is the field's type. The generated setter invokes it before assignment (after the
/// equality check); it may coerce the incoming value through the ref parameter, and returning
/// false rejects the change entirely. The field still holds the old value while the gate
/// runs.
/// </para>
/// <para>
/// <c>fieldChanged</c> names a change callback with the signature
/// <c>void Method(T oldValue, T newValue)</c>; it is invoked by the generated setter after
/// assignment.
/// <code>
/// [SerializableField(2, allowFieldChange: nameof(AllowLevelChange), fieldChanged: nameof(OnLevelChanged))]
/// private int _level;
///
/// private bool AllowLevelChange(ref int value)
/// {
///     value = Math.Clamp(value, 0, 100);
///     return true;
/// }
/// </code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SerializableFieldAttribute : Attribute
{
    public int Order { get; }
    public string PropertyGetter { get; }
    public string? PropertySetter { get; }
    public bool IsVirtual { get; }
    public string? FieldChanged { get; }
    public string? AllowFieldChange { get; }

    public SerializableFieldAttribute(
        int order,
        string getter = "public",
        string setter = "public",
        bool isVirtual = false,
        string fieldChanged = null,
        string allowFieldChange = null
    )
    {
        Order = order;
        PropertyGetter = getter;
        PropertySetter = setter;
        IsVirtual = isVirtual;
        FieldChanged = fieldChanged;
        AllowFieldChange = allowFieldChange;
    }
}
