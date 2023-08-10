/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NoRuleFoundException.cs                                         *
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

namespace ModernUO.Serialization.Generator;

public sealed class NoRuleFoundException : Exception
{
    public string PropertyName { get; }
    public string PropertyType { get; }

    public NoRuleFoundException(string propertyName, string propertyType)
        : base(ExceptionMessage(propertyName, propertyType))
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    public NoRuleFoundException(string propertyName, string propertyType, Exception innerException)
        : base(ExceptionMessage(propertyName, propertyType), innerException)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    private static string ExceptionMessage(string propertyName, string propertyType) =>
        $"No rule found for property {propertyName} of type {propertyType}.";
}
