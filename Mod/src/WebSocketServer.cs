using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GameplayNotIncluded
{
    public class WebSocketServer
    {
        private TcpListener _listener;
        private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new ConcurrentDictionary<Guid, TcpClient>();
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _messageEvent = new AutoResetEvent(false);
        private Thread _broadcastThread;

        public void Start(int port)
        {
            _cts = new CancellationTokenSource();
            
            // Listen on both IPv4 and IPv6 loopback interfaces
            _listener = new TcpListener(IPAddress.IPv6Any, port);
            try
            {
                _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[GameplayNotIncluded] Failed to set dual-stack socket option: " + ex.Message);
            }
            _listener.Start();

            // Accept connections asynchronously
            Task.Run(() => AcceptConnectionsAsync(_cts.Token));

            // Start background broadcast thread
            _broadcastThread = new Thread(BroadcastLoop)
            {
                IsBackground = true,
                Name = "GameplayNotIncluded_BroadcastThread"
            };
            _broadcastThread.Start();
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); } catch {}

            _messageEvent.Set();
            if (_broadcastThread != null && _broadcastThread.IsAlive)
            {
                _broadcastThread.Join(1000);
            }

            foreach (var client in _clients.Values)
            {
                try { client.Close(); } catch {}
            }
            _clients.Clear();
        }

        public void EnqueueMessage(string json)
        {
            _messageQueue.Enqueue(json);
            _messageEvent.Set();
        }

        private void BroadcastLoop()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _messageEvent.WaitOne();
                if (_cts.Token.IsCancellationRequested) break;

                while (_messageQueue.TryDequeue(out string message))
                {
                    BroadcastToClients(message);
                }
            }
        }

        private void BroadcastToClients(string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            byte[] frame = CreateTextFrame(payload);

            foreach (var pair in _clients)
            {
                var client = pair.Value;
                try
                {
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        stream.Write(frame, 0, frame.Length);
                        stream.Flush();
                    }
                    else
                    {
                        _clients.TryRemove(pair.Key, out _);
                        try { client.Close(); } catch {}
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[GameplayNotIncluded] Error sending to client: " + ex.Message);
                    _clients.TryRemove(pair.Key, out _);
                    try { client.Close(); } catch {}
                }
            }
        }

        private byte[] CreateTextFrame(byte[] payload)
        {
            byte[] header;
            int length = payload.Length;

            if (length <= 125)
            {
                header = new byte[2];
                header[0] = 0x81; // FIN + Text Opcode
                header[1] = (byte)length;
            }
            else if (length <= 65535)
            {
                header = new byte[4];
                header[0] = 0x81;
                header[1] = 126;
                header[2] = (byte)((length >> 8) & 0xFF);
                header[3] = (byte)(length & 0xFF);
            }
            else
            {
                header = new byte[10];
                header[0] = 0x81;
                header[1] = 127;
                // 8 bytes length (big-endian)
                for (int i = 0; i < 8; i++)
                {
                    header[2 + i] = (byte)((length >> (56 - i * 8)) & 0xFF);
                }
            }

            byte[] frame = new byte[header.Length + payload.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            Buffer.BlockCopy(payload, 0, frame, header.Length, payload.Length);
            return frame;
        }

        private async Task AcceptConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = ProcessClientAsync(client);
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ProcessClientAsync(TcpClient client)
        {
            Guid clientId = Guid.NewGuid();
            try
            {
                var stream = client.GetStream();
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Verify WebSocket upgrade request
                if (request.Contains("GET") && request.IndexOf("Upgrade: websocket", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var match = Regex.Match(request, @"Sec-WebSocket-Key:\s*(.*)");
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value.Trim();
                        string acceptKey = ComputeAcceptKey(key);

                        string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                                          "Upgrade: websocket\r\n" +
                                          "Connection: Upgrade\r\n" +
                                          "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n";

                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length).ConfigureAwait(false);
                        await stream.FlushAsync().ConfigureAwait(false);

                        _clients.TryAdd(clientId, client);
                        UnityEngine.Debug.Log($"[GameplayNotIncluded] Client connected. Total clients: {_clients.Count}");

                        // Keep connection open and read (to detect disconnects)
                        while (client.Connected)
                        {
                            int read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            if (read == 0) break; // connection closed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[GameplayNotIncluded] WebSocket client error: " + ex.Message);
            }
            finally
            {
                _clients.TryRemove(clientId, out _);
                try { client.Close(); } catch {}
                UnityEngine.Debug.Log($"[GameplayNotIncluded] Client disconnected. Total clients: {_clients.Count}");
            }
        }

        private string ComputeAcceptKey(string key)
        {
            string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string concatenated = key + guid;
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
