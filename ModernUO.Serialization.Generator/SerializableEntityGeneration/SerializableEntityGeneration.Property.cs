/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableEntityGeneration.Property.cs                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SerializableEntityGeneration
{
    public static void GenerateSerializableProperty(
        this StringBuilder source,
        string indent,
        FieldPropertyModel field,
        string? markDirtyMethod
    )
    {
        var fieldName = field.FieldName;

        var propertyIndent = $"{indent}    ";
        var innerIndent = $"{propertyIndent}    ";

        var setter = field.Setter;
        var propertyAccessor = setter > field.Getter ? setter : field.Getter;
        var getterAccessor = field.Getter == propertyAccessor ? Accessibility.NotApplicable : field.Getter;

        source.GeneratePropertyStart(indent, propertyAccessor.Value, field.Virtual, field.FieldTypeDisplay, field.PropertyName);

        // Getter
        source.GeneratePropertyGetterReturnsField(propertyIndent, fieldName, getterAccessor);

        if (setter != null && setter != Accessibility.NotApplicable)
        {
            var setterAccessor = setter == propertyAccessor ? Accessibility.NotApplicable : setter;

            // Setter
            source.GeneratePropertySetterStart(propertyIndent, false, setterAccessor.Value);

            // Capture old value before comparison if we have a changed callback
            if (field.FieldChangedMethodName != null)
            {
                source.AppendLine($"{innerIndent}var oldValue = {fieldName};");
            }

            var comparison = field.HasInequalityOperator
                ? $"value != {fieldName}"
                : $"!System.Collections.Generic.EqualityComparer<{field.FieldTypeDisplay}>.Default.Equals(value, {fieldName})";

            source.AppendLine($"{innerIndent}if ({comparison})");
            source.AppendLine($"{innerIndent}{{");
            source.AppendLine($"{innerIndent}    {fieldName} = value;");
            if (markDirtyMethod != null)
            {
                source.AppendLine($"{innerIndent}    {markDirtyMethod};");
            }

            if (field.InvalidateProperties)
            {
                source.AppendLine($"{innerIndent}    InvalidateProperties();");
            }

            // Invoke the changed callback after assignment
            if (field.FieldChangedMethodName != null)
            {
                source.AppendLine($"{innerIndent}    {field.FieldChangedMethodName}(oldValue, value);");
            }

            source.AppendLine($"{innerIndent}}}");
            source.GeneratePropertyGetSetEnd(propertyIndent, false);
        }

        source.GeneratePropertyEnd(indent);
    }
}
