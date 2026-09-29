using System.Buffers.Binary;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

/// <summary>Small SNTP codec. Wire timestamps are UTC, never local/KST fields.</summary>
internal static class NtpPacket
{
    private static readonly DateTimeOffset Epoch = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const long EraSeconds = 1L << 32;

    public static byte[] CreateRequest(DateTimeOffset utc)
    {
        var packet = new byte[48];
        packet[0] = 0x23; // LI=0, VN=4, mode=3 (client).
        var ticks = (utc - Epoch).Ticks;
        var seconds = Math.DivRem(ticks, TimeSpan.TicksPerSecond, out var fraction);
        if (fraction < 0) { seconds--; fraction += TimeSpan.TicksPerSecond; }
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(40), unchecked((uint)seconds));
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(44), (uint)((fraction << 32) / TimeSpan.TicksPerSecond));
        return packet;
    }

    public static DateTimeOffset ReadTimestamp(ReadOnlySpan<byte> bytes, DateTimeOffset reference)
    {
        if (bytes.Length < 8) throw new FormatException("Short NTP timestamp.");
        var seconds = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        var fraction = BinaryPrimitives.ReadUInt32BigEndian(bytes[4..]);
        var referenceSeconds = (reference - Epoch).Ticks / TimeSpan.TicksPerSecond;
        var era = (long)Math.Round((referenceSeconds - seconds) / (double)EraSeconds);
        var ticks = checked((era * EraSeconds + seconds) * TimeSpan.TicksPerSecond
            + (long)(((ulong)fraction * TimeSpan.TicksPerSecond) >> 32));
        try { return Epoch.AddTicks(ticks); }
        catch (ArgumentOutOfRangeException error) { throw new FormatException("NTP era outside supported date range.", error); }
    }

    public static NtpSample Parse(byte[] packet, byte[] request, DateTimeOffset t1, DateTimeOffset t4, long receivedTimestamp)
    {
        if (packet.Length < 48 || request.Length != 48) throw new FormatException("Short NTP packet.");
        var version = (packet[0] >> 3) & 7;
        if (version is not (3 or 4) || (packet[0] & 7) != 4 || (packet[0] >> 6) == 3
            || packet[1] is < 1 or > 15)
            throw new FormatException("Unsuitable NTP server response.");
        if (!packet.AsSpan(24, 8).SequenceEqual(request.AsSpan(40, 8)))
            throw new FormatException("NTP originate mismatch.");
        if (BinaryPrimitives.ReadUInt64BigEndian(packet.AsSpan(32)) == 0
            || BinaryPrimitives.ReadUInt64BigEndian(packet.AsSpan(40)) == 0)
            throw new FormatException("Missing NTP timestamp.");
        var t2 = ReadTimestamp(packet.AsSpan(32), t1);
        var t3 = ReadTimestamp(packet.AsSpan(40), t2);
        var delay = (t4 - t1) - (t3 - t2);
        if (t3 < t2 || t4 < t1 || delay < TimeSpan.Zero || delay > NtpSample.MaximumDelay)
            throw new FormatException("Unsuitable NTP timing.");
        var offset = TimeSpan.FromTicks(((t2 - t1).Ticks + (t3 - t4).Ticks) / 2);
        return new(offset, delay, t4 + offset, receivedTimestamp);
    }
}
