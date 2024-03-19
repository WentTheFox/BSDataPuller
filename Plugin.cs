using System;
using System.Reflection;
using IPA;
using IPALogger = IPA.Logging.Logger;
using SiraUtil.Zenject;
using DataPuller.Installers;
using DataPuller.Data;
using IPA.Loader;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

#nullable enable
namespace DataPuller
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public const string PLUGIN_NAME = "datapuller";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Logger { get; private set; }
        internal Server.Server webSocketServer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal static readonly HarmonyLib.Harmony harmony = new($"com.readiefur.{PLUGIN_NAME}");

        [Init]
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            Instance = this;
            Logger = logger;
            Logger.Debug("Logger initialized.");

            zenjector.Install<AppInstallers>(Location.App);
            zenjector.Install<PlayerInstallers>(Location.Player);
            zenjector.Install<MenuInstallers>(Location.Menu);
            zenjector.Expose<ScoreUIController>($"{PLUGIN_NAME}_{nameof(ScoreUIController)}");

            Logger.Debug("Apply Harmony patches");
            try { harmony.PatchAll(Assembly.GetExecutingAssembly()); }
            catch (Exception ex) { Logger.Debug(ex); }

            webSocketServer = new();

            PluginManager.OnAnyPluginsStateChanged += HandlePluginsStateChanged;
            HandlePluginsStateChanged();
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Logger.Debug("OnApplicationStart");
            HandlePluginsStateChanged();
        }

        internal void HandlePluginsStateChanged(Task changeTask, IEnumerable<PluginMetadata> enabled, IEnumerable<PluginMetadata> disabled)
        {
            HandlePluginsStateChanged();
        }

        internal void HandlePluginsStateChanged()
        {
            Logger.Debug("HandlePluginsStateChanged");
            ModData.Instance.EnabledPlugins = PluginManager.EnabledPlugins.ToList().ConvertAll(enabledPlugin => new SPluginMetadata
            {
                Author = enabledPlugin.Author,
                Name = enabledPlugin.Name,
                Version = $"{enabledPlugin.HVersion.Major}.{enabledPlugin.HVersion.Minor}.{enabledPlugin.HVersion.Patch}",
                Description = enabledPlugin.Description,
                HomeLink = enabledPlugin.PluginHomeLink == null ? "" : enabledPlugin.PluginHomeLink.ToString(),
                SourceLink = enabledPlugin.PluginSourceLink == null ? "" : enabledPlugin.PluginSourceLink.ToString(),
                DonateLink = enabledPlugin.DonateLink == null ? "" : enabledPlugin.DonateLink.ToString(),
            });
            ModData.Instance.Send();
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            webSocketServer?.Dispose();

            PluginManager.OnAnyPluginsStateChanged -= HandlePluginsStateChanged;

            Logger.Debug("Remove Harmony patches");
            try { harmony.UnpatchSelf(); }
            catch (Exception ex) { Logger.Debug(ex); }

            Logger.Debug("OnApplicationQuit");
        }
    }
}
