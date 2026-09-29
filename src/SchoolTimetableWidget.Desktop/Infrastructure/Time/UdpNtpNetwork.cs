using System.Net;
using System.Net.Sockets;

namespace SchoolTimetableWidget.Desktop.Infrastructure.Time;

internal sealed class UdpNtpNetwork(TimeProvider time) : INtpNetwork
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken) =>
        Dns.GetHostAddressesAsync(host, cancellationToken);

    public async Task<NtpDatagram> ExchangeAsync(IPAddress address, byte[] request, CancellationToken cancellationToken)
    {
        using var udp = new UdpClient(address.AddressFamily);
        // Connected UDP filters unrelated source addresses/ports at the socket boundary too.
        udp.Connect(new IPEndPoint(address, 123));
        await udp.SendAsync(request.AsMemory(), cancellationToken).ConfigureAwait(false);
        var response = await udp.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        return new(response.Buffer, response.RemoteEndPoint, time.GetTimestamp());
    }
}
