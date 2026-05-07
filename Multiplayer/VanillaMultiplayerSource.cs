using DataPuller.Data;
using IPA.Loader;
using System;
using System.Linq;
using System.Reflection;
using Zenject;

#nullable enable
namespace DataPuller.Multiplayer
{
    internal class VanillaMultiplayerSource : IMultiplayerSource
    {
        private const string MultiplayerCoreId = "MultiplayerCore";

        private BeatSaberConnectedPlayerManager? _connectedPlayerManager;
        private int _maxPlayerCount;

        internal void Activate(BeatSaberConnectedPlayerManager cpm)
        {
            _connectedPlayerManager = cpm;
            _connectedPlayerManager.connectedEvent += OnConnected;
            _connectedPlayerManager.disconnectedEvent += OnDisconnected;
            _connectedPlayerManager.playerConnectedEvent += OnPlayerChanged;
            _connectedPlayerManager.playerDisconnectedEvent += OnPlayerChanged;

            MapData.Instance.IsMultiplayer = true;
            ApplySource();
            UpdatePlayerCount();
        }

        public void Deactivate()
        {
            if (_connectedPlayerManager is null) return;

            MapData.Instance.IsMultiplayer = false;
            MapData.Instance.MultiplayerLobbyJoinCode = null;
            MapData.Instance.MultiplayerLobbySource = null;
            MapData.Instance.MultiplayerCoreLobbyMod = null;
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
            // By the time a lobby code is displayed the server status endpoint will have
            // been contacted. Re-apply source to pick up the mod name if it wasn't
            // available when Activate() ran.
            ApplySource();
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

        private static void ApplySource()
        {
            if (TryGetMultiplayerCoreSource(out var source, out var modName))
            {
                if (source != null)
                    MapData.Instance.MultiplayerLobbySource = source;
                MapData.Instance.MultiplayerCoreLobbyMod = modName;
                return;
            }

            MapData.Instance.MultiplayerLobbySource = MultiplayerLobbySourceType.Vanilla;
            MapData.Instance.MultiplayerCoreLobbyMod = null;
        }

        /// <summary>
        /// Attempts to detect the lobby source using MultiplayerCore's NetworkConfigPatcher.
        /// Returns false if MultiplayerCore is not installed or an error occurs.
        /// Returns true with a null <paramref name="source"/> if MultiplayerCore is overriding
        /// the API but the server name is not yet available from the status endpoint.
        /// When overriding, <paramref name="source"/> is <see cref="MultiplayerLobbySourceType.MultiplayerCore"/>
        /// and <paramref name="modName"/> is the server name from the status endpoint (e.g. "BeatTogether").
        /// </summary>
        private static bool TryGetMultiplayerCoreSource(out string? source, out string? modName)
        {
            source = null;
            modName = null;
            if (PluginManager.GetPluginFromId(MultiplayerCoreId) == null) return false;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == MultiplayerCoreId);
                if (asm == null) return false;

                var patcherType = asm.GetType("MultiplayerCore.Patchers.NetworkConfigPatcher");
                if (patcherType == null) return false;

                var patcher = ProjectContext.Instance.Container.TryResolve(patcherType);
                if (patcher == null) return false;

                var isOverriding = patcherType.GetProperty("IsOverridingApi")?.GetValue(patcher) as bool?;
                if (isOverriding == false)
                {
                    source = MultiplayerLobbySourceType.Vanilla;
                    return true;
                }

                if (isOverriding == true)
                {
                    source = MultiplayerLobbySourceType.MultiplayerCore;
                    modName = GetMultiplayerCoreServerName(asm, patcher, patcherType);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger.Debug($"MultiplayerCore source detection failed: {ex.Message}");
                return false;
            }
        }

        private static string? GetMultiplayerCoreServerName(Assembly asm, object patcher, Type patcherType)
        {
            try
            {
                var statusUrl = patcherType.GetProperty("MasterServerStatusUrl")?.GetValue(patcher) as string;
                if (string.IsNullOrEmpty(statusUrl)) return null;

                var repoType = asm.GetType("MultiplayerCore.Repositories.MpStatusRepository");
                if (repoType == null) return null;

                var repo = ProjectContext.Instance.Container.TryResolve(repoType);
                if (repo == null) return null;

                var status = repoType.GetMethod("GetStatusForUrl")?.Invoke(repo, new object[] { statusUrl });
                return status?.GetType().GetProperty("name")?.GetValue(status) as string;
            }
            catch (Exception ex)
            {
                Plugin.Logger.Debug($"MultiplayerCore server name lookup failed: {ex.Message}");
                return null;
            }
        }
    }
}
