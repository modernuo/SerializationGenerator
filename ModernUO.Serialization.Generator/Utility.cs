/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Utility.cs                                                      *
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
using System.Collections.Generic;
using System.Globalization;

namespace ModernUO.Serialization.Generator;

public static class Utility
{
    public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
    {
        key = tuple.Key;
        value = tuple.Value;
    }

    public static T RunAsEnglish<T>(Func<T> action)
    {
        var originalCulture = CultureInfo.DefaultThreadCurrentCulture;

        try
        {
            // Temporarily set culture to English
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            // Execute the action
            return action();
        }
        finally
        {
            // Restore the original culture
            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
        }
    }
}
