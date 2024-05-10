using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using Newtonsoft.Json;
using WestReportSystemApiReborn;

namespace WestReportChatNotify
{
    public class WestReportChatNotify : BasePlugin
    {
        public override string ModuleName => "WestReportChatNotify";
        public override string ModuleVersion => "v1.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Module that adds a reminder to use the report command";

        private IWestReportSystemApi? WRS_API;
        private ChatNotifyConfig? _config;
        private bool messageToHudEnabled = false;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            string configDirectory = GetConfigDirectory();
            EnsureConfigDirectory(configDirectory);
            string configPath = Path.Combine(configDirectory, "ChatNotifyConfig.json");
            _config = ChatNotifyConfig.Load(configPath);

            WRS_API = IWestReportSystemApi.Capability.Get();
            if (WRS_API == null)
            {
                Console.WriteLine($"{ModuleName} | Error: WestReportSystem API is not available.");
                return;
            }

            InitializeChatNotify();
        }

        private static string GetConfigDirectory()
        {
            return Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/configs/plugins/WestReportSystem/Modules");
        }

        private void EnsureConfigDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"{ModuleName} | Created configuration directory at: {directoryPath}");
            }
        }

        private void InitializeChatNotify()
        {
            if (_config == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Configuration is not loaded.");
                return;
            }

            string[] commands = WRS_API?.GetConfigValue<string[]>("Commands") ?? [];
            float timer = _config.ChatNotifyTimer;
            int mode = _config.ChatNotifyMode;

            if (commands.Length == 0)
            {
                Console.WriteLine($"{ModuleName} | Configuration error: Commands are not defined.");
                return;
            }

            if (timer == 0 || mode == 0)
            {
                Console.WriteLine($"{ModuleName} | Configuration error: Check timer and mode settings.");
                return;
            }

            if (mode == 1)
            {
                AddTimer(timer, () => SendMessageToChat(commands[0]), TimerFlags.REPEAT);
                Console.WriteLine($"{ModuleName} | Initialized with Timer: {timer}s, Mode: CHAT, Command: {commands[0]}");
            }
            else if (mode == 2)
            {
                float viewtimer = WRS_API?.GetConfigValue<float?>("ChatNotifyDuration") ?? 5.0f;
                RegisterListener<Listeners.OnTick>(() =>
                {
                    if (messageToHudEnabled)
                    {
                        Utilities.GetPlayers().Where(IsValidPlayer).ToList().ForEach(p => OnTick(p, commands[0]));
                    }
                });
                AddTimer(timer, () => ToggleMessageToHud(viewtimer), TimerFlags.REPEAT);
                Console.WriteLine($"{ModuleName} | Initialized with Timer: {timer}s, Mode: HUD, Command: {commands[0]}, View: {viewtimer}s");
            }
        }

        private void SendMessageToChat(string command)
        {
            Server.PrintToChatAll($"{WRS_API?.GetTranslatedText("wrcn.ChatMessage", WRS_API.GetTranslatedText("wrs.Prefix"), command)}");
        }

        private void ToggleMessageToHud(float viewtimer)
        {
            messageToHudEnabled = true;
            AddTimer(viewtimer, () => { messageToHudEnabled = false; });
        }

        private void OnTick(CCSPlayerController player, string command)
        {
            player.PrintToCenterHtml($"{WRS_API?.GetTranslatedText("wrcn.HudMessage", command)}");
        }

        private bool IsValidPlayer(CCSPlayerController player)
        {
            return player.IsValid && player.PlayerPawn?.IsValid == true && !player.IsBot && !player.IsHLTV && player.Connected == PlayerConnectedState.PlayerConnected;
        }

        public class ChatNotifyConfig
        {
            public float ChatNotifyTimer { get; set; } = 60.0f;
            public int ChatNotifyMode { get; set; } = 1;
            public float ChatNotifyDuration { get; set; } = 5.0f;

            public static ChatNotifyConfig Load(string configPath)
            {
                if (!File.Exists(configPath))
                {
                    ChatNotifyConfig defaultConfig = new();
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented));
                    return defaultConfig;
                }

                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<ChatNotifyConfig>(json) ?? new ChatNotifyConfig();
            }
        }
    }
}