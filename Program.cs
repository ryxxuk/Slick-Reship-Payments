using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SlickReship_Payments.Functions;

namespace SlickReship_Payments
{
    public class Program
    {
        private static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            // Get the bot token from the Config.json file.
            var config = DiscordFunctions.GetConfig();
            var token = config["token"].Value<string>();

            var services = ConfigureServices();

            var client = services.GetRequiredService<DiscordSocketClient>();
            services.GetRequiredService<CommandService>();
            services.GetRequiredService<LoggingService>();

            // Log in to Discord and start the bot.
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            client.PurgeUserCache();
            await client.DownloadUsersAsync(client.Guilds);

            // Run the bot forever.
            await Task.Delay(-1);
        }

        public static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Info,
                    DefaultRunMode = RunMode.Async,
                    CaseSensitiveCommands = false
                }))

                .AddSingleton<CommandHandlingService>()
                .AddSingleton<LoggingService>()
                .BuildServiceProvider();
        }
    }
}