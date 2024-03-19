using DataPuller.Core;
using DataPuller.Data;
using Zenject;

namespace DataPuller.Installers
{
    internal class MenuInstallers : MonoInstaller
    {
        public override void InstallBindings()
        {
            Plugin.Logger.Debug($"{nameof(MenuInstallers)} InstallBindings.");
            Container.BindInterfacesAndSelfTo<LocalLeaderboardEvents>().AsSingle();
        }
    }
}
