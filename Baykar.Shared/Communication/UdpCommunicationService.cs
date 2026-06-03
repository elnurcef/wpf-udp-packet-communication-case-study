using System.Net.Sockets;

namespace Baykar.Shared.Communication;

public sealed class UdpCommunicationService : IDisposable
{
    private const int ConnectionResetErrorCode = 10054;

    private readonly object _syncRoot = new();
    private UdpClient? _udpClient;
    private bool _stopRequested;
    private bool _disposed;

    public event EventHandler<byte[]>? BytesReceived;

    public async Task StartListeningAsync(int localPort, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        UdpClient client = new(localPort);

        lock (_syncRoot)
        {
            if (_udpClient is not null)
            {
                client.Dispose();
                throw new InvalidOperationException("UDP listener is already running.");
            }

            _stopRequested = false;
            _udpClient = client;
        }

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(StopListening);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await client.ReceiveAsync(cancellationToken);
                    BytesReceived?.Invoke(this, result.Buffer);
                }
                catch (SocketException exception) when (IsConnectionReset(exception))
                {
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested || _disposed || _stopRequested)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested || _disposed || _stopRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            lock (_syncRoot)
            {
                if (ReferenceEquals(_udpClient, client))
                {
                    _udpClient = null;
                }

                _stopRequested = false;
            }

            client.Dispose();
        }
    }

    public async Task SendAsync(byte[] data, string remoteIp, int remotePort)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteIp);
        ObjectDisposedException.ThrowIf(_disposed, this);

        UdpClient? listenerClient;

        lock (_syncRoot)
        {
            listenerClient = _udpClient;
        }

        if (listenerClient is not null)
        {
            await listenerClient.SendAsync(data, data.Length, remoteIp, remotePort);
            return;
        }

        using UdpClient sender = new();
        await sender.SendAsync(data, data.Length, remoteIp, remotePort);
    }

    public void StopListening()
    {
        lock (_syncRoot)
        {
            _stopRequested = true;
            _udpClient?.Dispose();
            _udpClient = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        StopListening();
    }

    private static bool IsConnectionReset(SocketException exception)
    {
        return exception.SocketErrorCode == SocketError.ConnectionReset
            || exception.ErrorCode == ConnectionResetErrorCode;
    }
}
