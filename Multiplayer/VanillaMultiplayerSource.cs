using DataPuller.Data;
using IPA.Loader;

#nullable enable
namespace DataPuller.Multiplayer
{
    internal class VanillaMultiplayerSource : IMultiplayerSource
    {
        private ConnectedPlayerManager? _connectedPlayerManager;
        private int _maxPlayerCount;

        internal void Activate(ConnectedPlayerManager cpm)
        {
            _connectedPlayerManager = cpm;
            _connectedPlayerManager.connectedEvent += OnConnected;
            _connectedPlayerManager.disconnectedEvent += OnDisconnected;
            _connectedPlayerManager.playerConnectedEvent += OnPlayerChanged;
            _connectedPlayerManager.playerDisconnectedEvent += OnPlayerChanged;

            MapData.Instance.IsMultiplayer = true;
            MapData.Instance.MultiplayerLobbySource = DetectSource();
            UpdatePlayerCount();
        }

        public void Deactivate()
        {
            if (_connectedPlayerManager is null) return;

            MapData.Instance.IsMultiplayer = false;
            MapData.Instance.MultiplayerLobbyJoinCode = null;
            MapData.Instance.MultiplayerLobbySource = null;
            MapData.Instance.MultiplayerLobbyIsPrivate = null;
            _maxPlayerCount = 0;

            _connectedPlayerManager.connectedEvent -= OnConnected;
            _connectedPlayerManager.disconnectedEvent -= OnDisconnected;
            _connectedPlayerManager.playerConnectedEvent -= OnPlayerChanged;
            _connectedPlayerManager.playerDisconnectedEvent -= OnPlayerChanged;
            _connectedPlayerManager = null;

            UpdatePlayerCount();
        }

        internal void SetMaxPlayerCount(int count)
        {
            _maxPlayerCount = count;
            UpdatePlayerCount();
        }

        internal void SetLobbyCode(string code)
        {
            MapData.Instance.MultiplayerLobbyJoinCode = code;
            MapData.Instance.Send();
        }

        public void RestoreSessionFields() => UpdatePlayerCount();

        private void UpdatePlayerCount()
        {
            MapData.Instance.MultiplayerLobbyMaxSize = _maxPlayerCount;
            MapData.Instance.MultiplayerLobbyCurrentSize = _connectedPlayerManager?.connectedPlayerCount ?? 0;
            MapData.Instance.Send();
        }

        private void OnConnected() => UpdatePlayerCount();
        private void OnDisconnected(DisconnectedReason _) => UpdatePlayerCount();
        private void OnPlayerChanged(object _) => UpdatePlayerCount();

        private static string DetectSource()
            => PluginManager.GetPluginFromId("BeatTogether") != null
                ? MultiplayerLobbySourceType.BeatTogether
                : MultiplayerLobbySourceType.Vanilla;
    }
}
