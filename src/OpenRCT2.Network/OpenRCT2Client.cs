using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpenRCT2.Network.Extensions;
using OpenRCT2.Network.Extensions.Endian;

namespace OpenRCT2.Network
{
    public class OpenRCT2Client : IDisposable
    {
        private const string NetworkVersion = "0.0.4-1";
        private const int PacketWaitTimeout = 12000;
        private const int ConnectionTimeout = 30000;
        public const int DefaultPort = 11753;

        private TcpClient _tcpClient;
        private string _userName;
        private byte _playerId;

        private bool _authenticationPending;
        private bool _authenticated;
        private AuthenticationResult _authenticationResult;
        private TaskSignal _authenticationReceivedSignal;
        private TaskSignal _serverInfoReceivedSignal;

        private DateTime _lastPingReceived;
        private Task _networkLoopTask;

        private string _serverInfoJson;

        public bool Connected { get; private set; }
        public bool ConnectionFailed { get; private set; }
        public Exception ConnectionException { get; private set; }

        public string DisconnectReason { get; private set; }

        public ImmutableArray<Player> Players { get; private set; }

        public event EventHandler Disconnected;
        public event EventHandler<IOpenRCT2String> ChatMessageReceived;
        public event EventHandler PlayerListUpdated;
        public event EventHandler<Player> PlayerJoined;
        public event EventHandler<Player> PlayerLeft;

        public OpenRCT2Client()
        {
            _tcpClient = new TcpClient();
            _tcpClient.NoDelay = true;
        }

        public void Dispose()
        {
#if NET451
            _tcpClient.Close();
#else
            _tcpClient.Dispose();
#endif
        }

        public async Task Connect(string host, int port)
        {
            await _tcpClient.ConnectAsync(host, port);

            // If connection failed, an exception would have occured
            Connected = true;
            _lastPingReceived = DateTime.Now;
            DisconnectReason = null;

            // Run the network loop on a background thread
            _networkLoopTask = Task.Run(() => Run());
        }

        public async Task<string> RequestServerInfo()
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not yet connected to a server.");
            }
            if (_serverInfoReceivedSignal != null)
            {
                throw new InvalidOperationException("Already requesting server info.");
            }

            _serverInfoReceivedSignal = new TaskSignal();
            try
            {
                SendServerInfoRequest();
                await _serverInfoReceivedSignal.Wait(PacketWaitTimeout);
                return _serverInfoJson;
            }
            finally
            {
                _serverInfoReceivedSignal = null;
            }
        }

        public async Task<AuthenticationResult> Authenticate(string userName, string password = null)
        {
            if (!Connected)
            {
                throw new InvalidOperationException("Not yet connected to a server.");
            }
            if (_authenticationPending || _authenticated)
            {
                throw new InvalidOperationException("Already or currently authenticating.");
            }

            _userName = userName;
            _authenticationPending = true;
            _authenticationReceivedSignal = new TaskSignal();

            try
            {
                SendAuth(userName, password);
                await _authenticationReceivedSignal.Wait(PacketWaitTimeout);
                return _authenticationResult;
            }
            finally
            {
                _authenticationPending = false;
                _authenticationReceivedSignal = null;
            }
        }

        private void Run()
        {
            var stream = _tcpClient.GetStream();
            var br = new BinaryReader(stream);
            while (IsStillConnected())
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        ushort packetLength = br.ReadUInt16()
                                                .ToLittleEndian();
                        byte[] payload = br.ReadBytes(packetLength);
                        OnNextPacket(payload);
                    }
                }
                catch { }
            }

            Connected = false;
            RaiseEvent(Disconnected);
        }

        private void OnNextPacket(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var br = new BinaryReader(ms);
                PacketType packetType = (PacketType)br.ReadUInt32()
                                                      .ToLittleEndian();

                switch (packetType) {
                case PacketType.Auth:
                    HandleAuth(br);
                    break;
                case PacketType.Chat:
                    HandleChat(br);
                    break;
                case PacketType.Ping:
                    HandlePing();
                    break;
                case PacketType.PlayerList:
                    HandlePlayerList(br);
                    break;
                case PacketType.SetDisconnectMessage:
                    HandleDisconnect(br);
                    break;
                case PacketType.ServerInfo:
                    HandleServerInfo(br);
                    break;
                }
            }
        }

        private void SendPacket(byte[] data)
        {
            var stream = _tcpClient.GetStream();
            var bw = new BinaryWriter(stream);

            ushort length = (ushort)data.Length;
            bw.Write(length.ToBigEndian());
            bw.Write(data);
        }

        private void HandlePing()
        {
            _lastPingReceived = DateTime.Now;

            using (var ms = new MemoryStream(capacity: 4))
            {
                var bw = new BinaryWriter(ms);
                bw.Write(PacketType.Ping);

                SendPacket(ms.ToArray());
            }
        }

        private void SendServerInfoRequest()
        {
            using (var ms = new MemoryStream(capacity: 4))
            {
                var bw = new BinaryWriter(ms);
                bw.Write(PacketType.ServerInfo);

                SendPacket(ms.ToArray());
            }
        }

        private void SendAuth(string userName, string password)
        {
            using (var ms = new MemoryStream(capacity: 128))
            {
                var bw = new BinaryWriter(ms);
                bw.Write(PacketType.Auth);
                bw.WriteUTF8(NetworkVersion);
                bw.WriteUTF8(userName);
                bw.WriteUTF8(password);

                SendPacket(ms.ToArray());
            }
        }

        public void SendChat(IOpenRCT2String message)
        {
            using (var ms = new MemoryStream())
            {
                var bw = new BinaryWriter(ms);
                bw.Write(PacketType.Chat);
                bw.WriteUTF8(message.Raw);

                SendPacket(ms.ToArray());
            }
        }

        private void HandleServerInfo(BinaryReader br)
        {
            _serverInfoJson = br.ReadUTF8();
            _serverInfoReceivedSignal?.Set();
        }

        private void HandleAuth(BinaryReader br)
        {
            uint authStatus = br.ReadUInt32()
                                .ToLittleEndian();
            byte playerId = br.ReadByte();

            _playerId = playerId;
            _authenticationResult = (AuthenticationResult)authStatus;
            _authenticated = _authenticationResult == AuthenticationResult.OK;
            _authenticationReceivedSignal?.Set();
        }

        private void HandleChat(BinaryReader br)
        {
            string chatMessage = br.ReadUTF8();
            IOpenRCT2String openRCT2string = new OpenRCT2String(chatMessage);
            RaiseEvent(ChatMessageReceived, openRCT2string);
        }

        private void HandlePlayerList(BinaryReader br)
        {
            ImmutableArray<Player> oldPlayers = Players;
            ImmutableArray<Player> newPlayers = new PlayerListPacket(br).Players;

            Players = newPlayers;
            RaiseEvent(PlayerListUpdated);

            // Detect addition or removal of players
            if (!oldPlayers.IsDefault)
            {
                // Get added and removed player ids
                var removedIds = oldPlayers.Select(x => x.Id)
                                           .Except(newPlayers.Select(y => y.Id))
                                           .ToArray();
                var removedPlayers = oldPlayers.Where(x => removedIds.Contains(x.Id))
                                               .ToArray();

                var addedIds = newPlayers.Select(x => x.Id)
                                         .Except(oldPlayers.Select(y => y.Id))
                                         .ToArray();
                var addedPlayers = newPlayers.Where(x => addedIds.Contains(x.Id))
                                             .ToArray();

                // Raise events
                foreach (var removedPlayer in removedPlayers)
                {
                    RaiseEvent(PlayerLeft, removedPlayer);
                }
                foreach (var addedPlayer in addedPlayers)
                {
                    RaiseEvent(PlayerJoined, addedPlayer);
                }
            }
        }

        private void HandleDisconnect(BinaryReader br)
        {
            string reason = br.ReadUTF8();
            DisconnectReason = reason;
        }

        private bool IsStillConnected()
        {
            // Check if TCP socket is still connected
            if (!_tcpClient.Connected)
            {
                return false;
            }

            // Have we been kicked?
            if (DisconnectReason != null)
            {
                return false;
            }

            // If we haven't received a PING in a long time, assume disconnected
            TimeSpan timeSinceLastPing = DateTime.Now - _lastPingReceived;
            if (timeSinceLastPing.TotalSeconds > ConnectionTimeout)
            {
                return false;
            }

            return true;
        }

        private void RaiseEvent(EventHandler handler)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseEvent<T>(EventHandler<T> handler, T packet)
        {
            handler?.Invoke(this, packet);
        }
    }
}
