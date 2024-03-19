using Zenject;
using DataPuller.Core;

namespace DataPuller.Installers
{
    internal class PlayerInstallers : MonoInstaller
    {
        public override void InstallBindings()
        {
            Plugin.Logger.Debug($"{nameof(PlayerInstallers)} InstallBindings.");
            Container.BindInterfacesAndSelfTo<MapEvents>().AsSingle();
        }
    }
}
