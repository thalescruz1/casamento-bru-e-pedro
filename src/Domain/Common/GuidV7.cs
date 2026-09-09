using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Casamento.Domain.Common;

/// <summary>
/// Implementação RFC 9562 v7: 48 bits de timestamp ms, versão 7, 62 bits random.
/// Substitui <c>Guid.CreateVersion7()</c> (disponível apenas em .NET 9+).
/// </summary>
public static class GuidV7
{
    public static Guid NewGuid() => NewGuid(DateTimeOffset.UtcNow);

    public static Guid NewGuid(DateTimeOffset timestamp)
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var unixMs = (ulong)timestamp.ToUnixTimeMilliseconds();
        // 48 bits de timestamp nos primeiros 6 bytes (big-endian).
        bytes[0] = (byte)(unixMs >> 40);
        bytes[1] = (byte)(unixMs >> 32);
        bytes[2] = (byte)(unixMs >> 24);
        bytes[3] = (byte)(unixMs >> 16);
        bytes[4] = (byte)(unixMs >> 8);
        bytes[5] = (byte)unixMs;

        // version 7 (high nibble do byte 6)
        bytes[6] = (byte)(0x70 | (bytes[6] & 0x0F));
        // variant RFC 4122 (bits 10xx nos 2 high bits do byte 8)
        bytes[8] = (byte)(0x80 | (bytes[8] & 0x3F));

        // Guid.Parse com mixed-endian (data1-3 são little-endian no wire format do Guid);
        // usar o construtor que recebe bytes assume mixed-endian; para preservar a ordem lida,
        // construímos via ReadOnlySpan<byte> com bigEndian: true.
        return new Guid(bytes, bigEndian: true);
    }
}
