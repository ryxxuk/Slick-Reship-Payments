using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlickReship_Payments.Functions;
using SlickReship_Payments.Modules;

namespace SlickReship_Payments
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        public static JObject _config;

        static Task Main(string[] args)
        {
            return new Program().MainAsync();
        }

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 250,
                AlwaysDownloadUsers = true
            });

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
            });

            _client.Log += Log;
            _commands.Log += Log;

            _config = DiscordFunctions.GetConfig();
        }

        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        private async Task MainAsync()
        {
            var token = _config["token"].Value<string>();

            await InitCommands();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            SlashCommands._client = _client;
            SlashCommands._config = _config;
            Database._config = _config;
            Functions.Stripe._config = _config;

            await Task.Delay(Timeout.Infinite);
        }

        private async Task InitCommands()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.Ready += CreateSlashCommands;
            _client.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "charge":
                    Console.WriteLine($"{DateTime.Now} [Command] {command.User.Username} Executed /charge");
                    await SlashCommands.Charge(command);
                    break;
                case "addstripe":
                    Console.WriteLine($"{DateTime.Now} [Command] {command.User.Username} Executed /addstripe");
                    await SlashCommands.AddStripe(command);
                    break;
                case "check":
                    Console.WriteLine($"{DateTime.Now} [Command] {command.User.Username} Executed /check");
                    await SlashCommands.Check(command);
                    break;
            }
        }

        public async Task CreateSlashCommands()
        {
            var slashCommandBuilders = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder()
                    .WithName("charge")
                    .WithDescription("Create a charge link for a customer")
                    .AddOption("customer", ApplicationCommandOptionType.User, "The customer you want to charge", isRequired: true)
                    .AddOption("customer", ApplicationCommandOptionType.User, "The reshipper who has the parcels", isRequired: true)
                    .AddOption("amount", ApplicationCommandOptionType.Number, "The amount to charge the customer", isRequired: true)
                    .AddOption("deliverycost", ApplicationCommandOptionType.Boolean, "Is the cost a delivery cost?, default false.", isRequired: false),
                new SlashCommandBuilder()
                    .WithName("addstripe")
                    .WithDescription("Adds a stripe account for a user")
                    .AddOption("user", ApplicationCommandOptionType.User, "The who who owns the connected account", isRequired: true)
                    .AddOption("stripeid", ApplicationCommandOptionType.String, "ID of the connected account on Stripe", isRequired: true),
                new SlashCommandBuilder()
                    .WithName("check")
                    .WithDescription("Checks all payment links in this channel.")
            };
            try
            {
                foreach (var slashCommand in slashCommandBuilders)
                {
                    await _client.Rest.CreateGuildCommand(slashCommand.Build(), _config["guild_id"].Value<ulong>());
                }
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Reason, Formatting.Indented);
                Console.WriteLine(json);
            }
        }
    }
}