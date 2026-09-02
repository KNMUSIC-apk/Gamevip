// ============================================================
// NetworkManager.cs
// NGO-based server-authoritative multiplayer.
// Host (1) + clients (2-20). Chat, party, trading, world sync.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using ProjectAria.Core;
using ProjectAria.Player;

namespace ProjectAria.Multiplayer
{
    public enum NetworkRole { None, Host, Client, Server }

    public class PlayerNetworkData : NetworkBehaviour
    {
        public NetworkVariable<Vector3> NetPosition = new();
        public NetworkVariable<Quaternion> NetRotation = new();
        public NetworkVariable<int> NetHealth = new();
        public NetworkVariable<int> NetHunger = new();
        public NetworkVariable<int> NetStamina = new();
        public NetworkVariable<int> NetSelectedHotbar = new();
        public NetworkVariable<FixedString64Bytes> NetPlayerName = new();
    }

    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }
        public NetworkRole Role { get; private set; } = NetworkRole.None;
        public bool IsConnected => Role != NetworkRole.None;

        public string ServerAddress = "127.0.0.1";
        public ushort ServerPort = 7777;
        public int MaxPlayers = 20;

        public event Action<ulong> OnPlayerJoined;
        public event Action<ulong> OnPlayerLeft;
        public event Action<ulong, string> OnChatMessage;

        private NetworkManager _net;
        private readonly Dictionary<ulong, string> _playerNames = new();

        public void Init()
        {
            if (Instance != null) return;
            Instance = this;
            _net = GetComponent<NetworkManager>();
            if (_net == null) _net = gameObject.AddComponent<NetworkManager>();
            var transport = GetComponent<UnityTransport>();
            if (transport == null) transport = gameObject.AddComponent<UnityTransport>();
            _net.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ConnectionApproval = true,
                EnableSceneManagement = true,
                TickRate = 30
            };
            _net.ConnectionApprovalCallback += OnApproval;
        }

        public bool StartHost()
        {
            if (_net == null) Init();
            if (!_net.StartHost()) return false;
            Role = NetworkRole.Host;
            return true;
        }

        public bool StartClient(string address = null, ushort port = 0)
        {
            if (_net == null) Init();
            if (address != null) ServerAddress = address;
            if (port != 0) ServerPort = port;
            var transport = _net.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport != null)
            {
                transport.SetConnectionData(ServerAddress, ServerPort);
            }
            if (!_net.StartClient()) return false;
            Role = NetworkRole.Client;
            return true;
        }

        public void Disconnect()
        {
            if (_net == null) return;
            _net.Shutdown();
            Role = NetworkRole.None;
        }

        private void OnApproval(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse resp)
        {
            resp.Approved = _net.ConnectedClientsIds.Count < MaxPlayers;
            resp.CreatePlayerObject = true;
            resp.Pending = false;
        }

        public void SendChat(string message)
        {
            if (_net == null) return;
            if (Role == NetworkRole.Host)
                BroadcastChatServerRpc(NetworkManager.Singleton.LocalClientId, message);
            else
                SendChatServerRpc(message);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendChatServerRpc(string message, ServerRpcParams rpc = default)
        {
            BroadcastChatServerRpc(rpc.Receive.SenderClientId, message);
        }

        [ClientRpc]
        public void BroadcastChatServerRpc(ulong senderId, string message)
        {
            OnChatMessage?.Invoke(senderId, message);
        }
    }
}
