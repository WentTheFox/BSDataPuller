using DataPuller.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable
namespace DataPuller.Multiplayer
{
    internal class BspMultiplayerSource : IMultiplayerSource
    {
        private static BspMultiplayerSource? _instance;

        private System.Timers.Timer? _timer;
        private string? _joinCode;
        private bool? _isPrivate;
        private int _maxPlayers;
        private int _currentPlayers;

        internal void Activate()
        {
            _instance = this;
            TryPatchNetworkManager();
            // Timer is a safety net — polls are cheap and idempotent.
            _timer = new System.Timers.Timer(5000) { AutoReset = true };
            _timer.Elapsed += (_, _) => PollState();
            _timer.Start();
        }

        public void Deactivate()
        {
            _instance = null;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        public void RestoreSessionFields()
        {
            if (_joinCode == null) return;
            MapData.Instance.IsMultiplayer = true;
            MapData.Instance.MultiplayerLobbySource = MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer;
            MapData.Instance.MultiplayerLobbyJoinCode = _joinCode;
            MapData.Instance.MultiplayerLobbyIsPrivate = _isPrivate;
            MapData.Instance.MultiplayerLobbyMaxSize = _maxPlayers;
            MapData.Instance.MultiplayerLobbyCurrentSize = _currentPlayers;
            MapData.Instance.Send();
        }

        // Static entry points for Harmony postfixes — delegates to the live instance.
        internal static void RoomState_Postfix() => _instance?.PollState();
        internal static void RoomClear_Postfix() { if (_instance?._joinCode != null) _instance.ClearSession(); }

        private void PollState()
        {
            if (!ReadRoomData(out var code, out var isPrivate, out var maxPlayers, out var currentPlayers) || string.IsNullOrEmpty(code))
            {
                if (_joinCode != null) ClearSession();
                return;
            }

            if (code == _joinCode && Nullable.Equals(isPrivate, _isPrivate) && maxPlayers == _maxPlayers && currentPlayers == _currentPlayers) return;

            _joinCode = code;
            _isPrivate = isPrivate;
            _maxPlayers = maxPlayers;
            _currentPlayers = currentPlayers;
            MapData.Instance.IsMultiplayer = true;
            MapData.Instance.MultiplayerLobbySource = MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer;
            MapData.Instance.MultiplayerLobbyJoinCode = _joinCode;
            MapData.Instance.MultiplayerLobbyIsPrivate = _isPrivate;
            MapData.Instance.MultiplayerLobbyMaxSize = _maxPlayers;
            MapData.Instance.MultiplayerLobbyCurrentSize = _currentPlayers;
            MapData.Instance.Send();
        }

        private void ClearSession()
        {
            _joinCode = null;
            _isPrivate = null;
            _maxPlayers = 0;
            _currentPlayers = 0;
            MapData.Instance.IsMultiplayer = false;
            MapData.Instance.MultiplayerLobbySource = null;
            MapData.Instance.MultiplayerLobbyJoinCode = null;
            MapData.Instance.MultiplayerLobbyIsPrivate = null;
            MapData.Instance.MultiplayerLobbyMaxSize = 0;
            MapData.Instance.MultiplayerLobbyCurrentSize = 0;
            MapData.Instance.Send();
        }

        // m_RoomData and RoomPlayerCount are non-public/public static members on NetworkManager.
        private static bool ReadRoomData(out string? code, out bool? isPrivate, out int maxPlayers, out int currentPlayers)
        {
            code = null;
            isPrivate = null;
            maxPlayers = 0;
            currentPlayers = 0;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer);
                if (asm == null) return false;

                var nmType = asm.GetType("BeatSaberPlus_Multiplayer.Network.NetworkManager");
                if (nmType == null) return false;

                var roomDataField = nmType.GetField("m_RoomData",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (roomDataField == null) return false;

                var roomData = roomDataField.GetValue(null);
                if (roomData == null) return false;

                var rdType = roomData.GetType();
                var rdFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                code = rdType.GetField("RoomCode", rdFlags)?.GetValue(roomData)?.ToString();

                var flagsField = rdType.GetField("RoomFlags", rdFlags);
                if (flagsField != null)
                    isPrivate = (Convert.ToInt32(flagsField.GetValue(roomData)) & 0x1) != 0;

                var maxPlayersField = rdType.GetField("MaxPlayers", rdFlags);
                if (maxPlayersField != null)
                    maxPlayers = Convert.ToInt32(maxPlayersField.GetValue(roomData));

                var playersDict = nmType.GetField("m_RoomPlayersD",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
                if (playersDict != null)
                    currentPlayers = Convert.ToInt32(playersDict.GetType()
                        .GetProperty("Count")?.GetValue(playersDict));

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger.Debug($"BSP read failed: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        // Patches methods directly on NetworkManager (confirmed via runtime reflection 2026-05-05).
        private static void TryPatchNetworkManager()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == MultiplayerLobbySourceType.BeatSaberPlus_Multiplayer);
                if (asm == null) return;

                var nmType = asm.GetType("BeatSaberPlus_Multiplayer.Network.NetworkManager");
                if (nmType == null) return;

                var statePostfix = new HarmonyMethod(typeof(BspMultiplayerSource), nameof(RoomState_Postfix));
                var clearPostfix = new HarmonyMethod(typeof(BspMultiplayerSource), nameof(RoomClear_Postfix));

                var updateNames = new HashSet<string> { "Handle_SMsgRoomState", "Handle_SMsgRoomUpdated", "Logic_OnRoomJoined", "Logic_OnRoomPlayerJoined", "Logic_OnRoomPlayerLeaved" };
                var clearNames  = new HashSet<string> { "Logic_OnRoomLeaved", "Handle_SMsgKickedFromRoom" };

                int patched = 0;
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                foreach (var m in nmType.GetMethods(flags))
                {
                    if (updateNames.Contains(m.Name))
                    { Plugin.harmony.Patch(m, postfix: statePostfix); patched++; }
                    else if (clearNames.Contains(m.Name))
                    { Plugin.harmony.Patch(m, postfix: clearPostfix); patched++; }
                }

                Plugin.Logger.Debug($"BSP: patched {patched} methods on NetworkManager.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.Debug($"BSP patch failed: {ex.Message}");
            }
        }
    }
}
