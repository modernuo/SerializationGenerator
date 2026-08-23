/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DeserializeTimerAttribute.cs                                    *
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
/// Declares how a serializable Timer field is restored at load. The named method must have the
/// signature <c>void Method(TimeSpan delay)</c> and is invoked only when a timer was running at
/// save, with its remaining delay.
/// <para>
/// By default the timer drifts: its next tick is written as anchored time, so downtime does not
/// consume the remaining delay. Set <paramref name="wallClock" /> to true for absolute
/// deadlines instead; the delay is then negative when the deadline passed during downtime.
/// </para>
/// <code>
/// [SerializableField(1)]
/// [DeserializeTimer(nameof(DeserializeRelockTimer))]
/// private Timer _relockTimer;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DeserializeTimerAttribute : Attribute
{
    public string MethodName { get; }

    public bool WallClock { get; }

    public DeserializeTimerAttribute(string methodName, bool wallClock = false)
    {
        MethodName = methodName;
        WallClock = wallClock;
    }
}
