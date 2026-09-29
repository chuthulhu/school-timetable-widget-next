using System.Net;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal sealed record NtpDatagram(byte[] Packet, IPEndPoint RemoteEndPoint, long ReceivedTimestamp);

internal interface INtpNetwork
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
    Task<NtpDatagram> ExchangeAsync(IPAddress address, byte[] request, CancellationToken cancellationToken);
}
