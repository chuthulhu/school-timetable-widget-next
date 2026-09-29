param()
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$probeDirectory = Join-Path ([IO.Path]::GetTempPath()) ('stw-kriss-probe-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDirectory | Out-Null
# Compile the actual production clock/codec/client/transport sources; no copied implementation.
$timeSource = [Security.SecurityElement]::Escape((Join-Path $repository 'src/SchoolTimetableWidget.Desktop/Infrastructure/Time/*.cs'))
$coreSource = [Security.SecurityElement]::Escape((Join-Path $repository 'src/SchoolTimetableWidget.Core/Time/*.cs'))
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Compile Include="' + $timeSource + '" /><Compile Include="' + $coreSource + '" /></ItemGroup></Project>'
[IO.File]::WriteAllText((Join-Path $probeDirectory 'Probe.csproj'), $project)
$program = @'
using System.Net;
using SchoolTimetableWidget.Desktop.Infrastructure.Time;

var time = TimeProvider.System;
var fallback = new PcFallbackApplicationClock();
var clock = new SynchronizedApplicationClock(fallback, time);
var network = new ObservedNetwork(time);
Console.WriteLine($"Observation UTC: {fallback.GetSnapshot().LocalTime.ToUniversalTime():O}");
Console.WriteLine($"Endpoint: {NtpClient.Host}:123; production sources; no profile/UI/system clock mutation");
var result = await new NtpClient(clock, time, network).SynchronizeAsync(CancellationToken.None);
if (result.Sample is { } sample)
{
    clock.Apply(sample);
    Console.WriteLine($"SUCCESS offset_ms={sample.Offset.TotalMilliseconds:F4} delay_ms={sample.Delay.TotalMilliseconds:F4} KST={clock.GetSnapshot().LocalTime:O}");
}
else Console.WriteLine($"UNAVAILABLE: {result.Failure}; PC fallback continues; direct NTP access not established in this environment.");
Console.WriteLine($"Valid production-parser responses: {network.ValidResponses}");

sealed class ObservedNetwork(TimeProvider time) : INtpNetwork
{
    private readonly UdpNtpNetwork inner = new(time);
    public int ValidResponses;
    public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken token)
    {
        var addresses = await inner.ResolveAsync(host, token);
        Console.WriteLine($"DNS resolved: {addresses.Length} address(es)");
        return addresses;
    }
    public async Task<NtpDatagram> ExchangeAsync(IPAddress address, byte[] request, CancellationToken token)
    {
        var started = time.GetTimestamp();
        var response = await inner.ExchangeAsync(address, request, token);
        var elapsed = time.GetElapsedTime(started, response.ReceivedTimestamp);
        var reference = new PcFallbackApplicationClock().GetSnapshot().LocalTime.ToUniversalTime();
        var t1 = NtpPacket.ReadTimestamp(request.AsSpan(40), reference);
        var sample = NtpPacket.Parse(response.Packet, request, t1, t1 + elapsed, response.ReceivedTimestamp);
        ValidResponses++;
        Console.WriteLine($"Valid response: RTT_ms={elapsed.TotalMilliseconds:F4} offset_ms={sample.Offset.TotalMilliseconds:F4} delay_ms={sample.Delay.TotalMilliseconds:F4}");
        return response;
    }
}
'@
[IO.File]::WriteAllText((Join-Path $probeDirectory 'Program.cs'), $program)
Write-Output "Isolated probe artifacts: $probeDirectory"
dotnet run --project (Join-Path $probeDirectory 'Probe.csproj') --verbosity quiet
exit $LASTEXITCODE
