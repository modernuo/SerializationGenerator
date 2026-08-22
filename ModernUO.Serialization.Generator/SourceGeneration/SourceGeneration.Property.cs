/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SourceGeneration.Property.cs                                    *
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
using System.Text;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace ModernUO.Serialization.Generator;

public static partial class SourceGeneration
{
    public static string GetPropertyName(this string fieldName)
    {
        var propertyName = fieldName;

        if (propertyName.StartsWith("m_", StringComparison.OrdinalIgnoreCase))
        {
            propertyName = propertyName.Substring(2);
        }
        else if (propertyName.StartsWith("_", StringComparison.OrdinalIgnoreCase))
        {
            propertyName = propertyName.Substring(1);
        }

        // Dehumanize converts "some text" to "SomeText", but in Humanizer 3.x it may insert
        // spaces before numbers (e.g., "slayer2" becomes "Slayer 2"). Remove any spaces since
        // C# identifiers cannot contain spaces.
        // See: https://github.com/Humanizr/Humanizer/issues/1656
        return Utility.RunAsEnglish(propertyName.Dehumanize).Replace(" ", "");
    }

    extension(StringBuilder source)
    {
        public void GeneratePropertyStart(
            string indent,
            Accessibility accessors,
            bool isVirtual,
            string typeDisplay,
            string propertyName
        )
        {
            var virt = isVirtual ? "virtual " : "";

            source.AppendLine($"{indent}{accessors.ToFriendlyString()} {virt}{typeDisplay} {propertyName}");
            source.AppendLine($"{indent}{{");
        }

        public void GenerateAutoProperty(
            Accessibility accessors,
            string type,
            string propertyName,
            Accessibility? getAccessor,
            Accessibility? setAccessor,
            string indent,
            bool useInit = false,
            string defaultValue = null,
            bool isOverride = false
        )
        {
            if (getAccessor == null && setAccessor == null)
            {
                throw new ArgumentNullException($"Must specify a {nameof(getAccessor)} or {nameof(setAccessor)} parameter");
            }

            var getter = getAccessor == null ?
                "" :
                $"{(getAccessor != Accessibility.NotApplicable ? $"{getAccessor.Value.ToFriendlyString()} " : "")}get;";

            var getterSpace = getAccessor != null ? " " : "";
            var setOrInit = useInit ? "init;" : "set;";

            var setterAccessor = setAccessor is null or Accessibility.NotApplicable
                ? ""
                : $"{setAccessor.Value.ToFriendlyString() ?? ""} ";

            var setter = setAccessor == null ? "" : $"{getterSpace}{setterAccessor}{setOrInit}";

            var propertyAccessor = accessors == Accessibility.NotApplicable ? "" : $"{accessors.ToFriendlyString()} ";
            var printOverride = isOverride ? "override " : "";
            var printDefaultValue = defaultValue != null ? $"{(setAccessor != null ? " =" : "")} {defaultValue};" : "";
            var printGetterSetter = setAccessor == null ? "=>" : $"{{ {getter}{setter} }}";

            source.AppendLine($"{indent}{propertyAccessor}{printOverride}{type} {propertyName} {printGetterSetter}{printDefaultValue}");
        }

        public void GeneratePropertyEnd(string indent) => source.AppendLine($"{indent}}}");

        public void GeneratePropertyGetterReturnsField(
            string indent,
            string fieldName,
            Accessibility Accessibility
        )
        {
            var accessor = Accessibility != Accessibility.NotApplicable ? $"{Accessibility.ToFriendlyString()} " : "";
            source.AppendLine($"{indent}{accessor}get => {fieldName};");
        }

        public void GeneratePropertyGetterStart(
            string indent,
            bool useExpression,
            Accessibility Accessibility
        )
        {
            var accessor = Accessibility != Accessibility.NotApplicable ? $"{Accessibility.ToFriendlyString()} " : "";
            var expression = useExpression ? " => " : $"\n{indent}{{";
            source.AppendLine($"{indent}{accessor}get{expression}");
        }

        public void GeneratePropertyGetSetEnd(string indent, bool useExpression)
        {
            if (!useExpression)
            {
                source.AppendLine($"{indent}}}");
            }
        }

        public void GeneratePropertySetterStart(
            string indent,
            bool useExpression,
            Accessibility Accessibility,
            bool useInit = false
        )
        {
            var init = useInit ? "init" : "set";
            var expression = useExpression ? " => " : $"\n{indent}{{";
            var accessor = Accessibility != Accessibility.NotApplicable ? $"{Accessibility.ToFriendlyString()} " : "";
            source.AppendLine($"{indent}{accessor}{init}{expression}");
        }
    }
}
