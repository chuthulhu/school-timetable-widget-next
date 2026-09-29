using System.Buffers.Binary;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

namespace SchoolTimetableWidget.Tests.Time;

public class NtpPacketTests
{
    internal static readonly DateTimeOffset Reference = new(2026, 9, 29, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RequestIsPrivateStandard48ByteClientPacketWithBigEndianTimestamp()
    {
        var packet = NtpPacket.CreateRequest(new DateTimeOffset(1900, 1, 1, 0, 0, 1, TimeSpan.Zero).AddMilliseconds(500));
        Assert.Equal(48, packet.Length);
        Assert.Equal(0x23, packet[0]);
        Assert.All(packet[1..40], b => Assert.Equal(0, b));
        Assert.Equal(new byte[] { 0, 0, 0, 1, 128, 0, 0, 0 }, packet[40..]);
    }

    [Theory]
    [InlineData("2026-09-29T00:00:00.1234567Z")]
    [InlineData("2036-02-07T06:28:15.9999999Z")]
    [InlineData("2036-02-07T06:28:16.0000001Z")]
    [InlineData("2040-01-01T00:00:00.5000000Z")]
    public void TimestampRoundTripsAroundReferenceAcrossEraRollover(string value)
    {
        var instant = DateTimeOffset.Parse(value);
        var packet = NtpPacket.CreateRequest(instant);
        Assert.InRange((NtpPacket.ReadTimestamp(packet.AsSpan(40), instant) - instant).Ticks, -1, 0);
    }

    [Fact]
    public void EraUsesReferenceRatherThanAlways1900()
    {
        var packet = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(packet, 5);
        Assert.Equal(DateTimeOffset.Parse("2036-02-07T06:28:21Z"), NtpPacket.ReadTimestamp(packet, DateTimeOffset.Parse("2037-01-01Z")));
    }

    [Theory]
    [InlineData(120, 40, 60, 110)]
    [InlineData(-120, 40, 60, -130)]
    [InlineData(0, 10, 90, -40)]
    [InlineData(0.125, 0.25, 0.25, 0.125)]
    public void FourTimestampsCalculateOffsetAndDelay(double serverOffsetMs, double outboundMs, double inboundMs, double expectedOffsetMs)
    {
        var t1 = Reference;
        var t2 = t1.AddMilliseconds(outboundMs + serverOffsetMs);
        var t3 = t2.AddMilliseconds(10);
        var t4 = t1.AddMilliseconds(outboundMs + inboundMs + 10);
        var request = NtpPacket.CreateRequest(t1);
        var sample = NtpPacket.Parse(Response(request, t2, t3), request, t1, t4, 123);
        Assert.InRange(Math.Abs(sample.Offset.TotalMilliseconds - expectedOffsetMs), 0, 0.0002);
        Assert.InRange(Math.Abs(sample.Delay.TotalMilliseconds - outboundMs - inboundMs), 0, 0.0002);
        Assert.Equal(t4 + sample.Offset, sample.UtcAtReceive);
        Assert.Equal(123, sample.ReceivedTimestamp);
    }

    [Theory]
    [InlineData(0, 0x04)] // unsupported version 0
    [InlineData(0, 0x2c)] // version 5
    [InlineData(0, 0x23)] // client mode
    [InlineData(0, 0xe4)] // unsynchronized
    [InlineData(1, 0)]
    [InlineData(1, 16)]
    [InlineData(1, 255)]
    [InlineData(24, 1)] // wrong originate
    public void InvalidHeaderAndOriginateAreRejected(int index, byte value)
    {
        var request = NtpPacket.CreateRequest(Reference);
        var response = Response(request, Reference, Reference);
        response[index] = value;
        Assert.Throws<FormatException>(() => NtpPacket.Parse(response, request, Reference, Reference, 0));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(47)]
    public void ShortPacketIsRejected(int length) => Assert.Throws<FormatException>(() =>
        NtpPacket.Parse(new byte[length], NtpPacket.CreateRequest(Reference), Reference, Reference, 0));

    [Theory]
    [InlineData(32)] [InlineData(40)]
    public void ZeroServerTimestampIsRejected(int index)
    {
        var request = NtpPacket.CreateRequest(Reference);
        var response = Response(request, Reference, Reference);
        response.AsSpan(index, 8).Clear();
        Assert.Throws<FormatException>(() => NtpPacket.Parse(response, request, Reference, Reference, 0));
    }

    [Theory]
    [InlineData(-1, 100)] // server reversed
    [InlineData(200, 100)] // negative delay
    [InlineData(0, 1001)] // slow network
    public void InvalidTimingIsRejected(int processingMs, int elapsedMs)
    {
        var request = NtpPacket.CreateRequest(Reference);
        Assert.Throws<FormatException>(() => NtpPacket.Parse(Response(request, Reference, Reference.AddMilliseconds(processingMs)),
            request, Reference, Reference.AddMilliseconds(elapsedMs), 0));
    }

    [Theory]
    [InlineData(0x1c)] [InlineData(0x24)] [InlineData(0x64)] [InlineData(0xa4)]
    public void SupportedVersionsAndSynchronizedLeapWarningsAreAccepted(byte header)
    {
        var request = NtpPacket.CreateRequest(Reference);
        var response = Response(request, Reference, Reference);
        response[0] = header;
        Assert.NotNull(NtpPacket.Parse(response, request, Reference, Reference.AddMilliseconds(100), 0));
    }

    internal static byte[] Response(byte[] request, DateTimeOffset receive, DateTimeOffset transmit)
    {
        var packet = new byte[48]; packet[0] = 0x24; packet[1] = 2;
        request.AsSpan(40, 8).CopyTo(packet.AsSpan(24));
        NtpPacket.CreateRequest(receive).AsSpan(40, 8).CopyTo(packet.AsSpan(32));
        NtpPacket.CreateRequest(transmit).AsSpan(40, 8).CopyTo(packet.AsSpan(40));
        return packet;
    }
}
