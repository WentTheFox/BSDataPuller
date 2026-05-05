#nullable enable
namespace DataPuller.Multiplayer
{
    internal interface IMultiplayerSource
    {
        void Deactivate();
        // Re-applies cached state to MapData after a MapData.Reset() during level load.
        void RestoreSessionFields();
    }
}
