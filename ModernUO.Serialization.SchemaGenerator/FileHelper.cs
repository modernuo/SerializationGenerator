/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FileHelper.cs                                                   *
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
using System.IO;
using System.Threading;

namespace ModernUO.Serialization.SchemaGenerator;

public class FileHelper
{
    public static long GetFileSize(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return -1;
        }

        try
        {
            return new FileInfo(filePath).Length;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    public static bool FileContentEquals(string filePath, ReadOnlySpan<byte> serializedBytes, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        int offset = 0;

        Span<byte> fileChunk = stackalloc byte[1024];

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        while (offset < serializedBytes.Length)
        {
            token.ThrowIfCancellationRequested();
            var bytesToRead = Math.Min(1024, serializedBytes.Length - offset);

            // Read a chunk from the file
            var bytesRead = fileStream.Read(fileChunk[..bytesToRead]);

            if (bytesRead != bytesToRead)
            {
                return false; // Mismatch in size
            }

            var serializedChunk = serializedBytes.Slice(offset, bytesRead);

            if (!fileChunk[..bytesRead].SequenceEqual(serializedChunk))
            {
                return false; // Mismatch found
            }

            offset += bytesRead;
        }

        return true; // All chunks matched
    }
}
