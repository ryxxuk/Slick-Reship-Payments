using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SlickReship_Payments.Functions;

namespace SlickReship_Payments.Modules
{
    class SlashCommands
    {
        public static DiscordSocketClient _client;
        public static JObject _config;

        public static async Task Charge(SocketSlashCommand command)
        {
            IUser reshipper = null;
            IUser customer = null;
            var deliveryCost = false;
            double amount = 0;

            var channel = (IMessageChannel)_client.Rest.GetChannelAsync(command.Channel.Id);

            foreach (var x in command.Data.Options.FirstOrDefault()?.Options)
            {
                switch (x.Name)
                {
                    case "customer":
                        customer = (IUser) x.Value;
                        break;
                    case "reshipper":
                        reshipper = (IUser) x.Value;
                        break;
                    case "amount":
                        amount = (double) x.Value;
                        break;
                    case "deliverycost":
                        deliveryCost = (bool) x.Value;
                        break;
                }
            }

            var restCustomer = await _client.Rest.GetGuildUserAsync(_config["guild_id"].Value<ulong>(), customer.Id);

            var stripeId = Database.GetStripeId(reshipper.Id);

            if (stripeId == "")
            {
                await channel.SendMessageAsync($"<@{reshipper.Id}> does not have an associated Stripe account. Use /addstripe to add their account.");
                return;
            }
            
            var tempMessage = await channel.SendMessageAsync("Generating Payment Session...");

            var transactionFee = amount / (1 - _config["stripe_percent_fee"].Value<double>()) - amount;
            transactionFee = Math.Round(transactionFee, 2) + 0;
            var applicationFee = restCustomer.RoleIds.Any(role => role == 896053430341222420) || deliveryCost ? transactionFee : (amount * 0.19) + transactionFee;
            
            var totalCost = amount + transactionFee;

            var stripeSession = await Functions.Stripe.CreateChargeSessionAsync(
                $"Reshipping Fee: £{amount}. Trans Fee: £{transactionFee}",
                totalCost,
                stripeId,
                applicationFee);

            var embed = EmbedTemplates.CreatePaymentEmbed(amount, transactionFee, stripeSession.Id);

            await channel.SendMessageAsync("", embed: embed);
            await tempMessage.DeleteAsync();

            Console.WriteLine($"Created new payment link for {stripeId}, total cost:{totalCost}, fee:{applicationFee}");
        }

        public async Task Check(SocketSlashCommand command)
        {
            var numberChecked = 0;

            var channel = (IMessageChannel) _client.Rest.GetChannelAsync(command.Channel.Id);

            var messages = channel.GetMessagesAsync().Flatten();

            await foreach (var message in messages)
            {
                if (message.Author.Id != _client.CurrentUser.Id) continue;
                if (message.Embeds.Count != 1) continue;
                if (message.Embeds.First().Title != "Payment Session Created") continue;
                if (message.Reactions.ContainsKey(new Emoji("✅"))) continue;

                var stripeSessionId = message.Embeds.First().Footer.ToString();

                var paid = await Functions.Stripe.CheckIfPaid(stripeSessionId);

                var embed = EmbedTemplates.CreatePaymentCheckEmbed(paid, stripeSessionId);

                await channel.SendMessageAsync("", embed: embed);

                if (paid)
                {
                    await message.AddReactionAsync(new Emoji("✅"));
                }

                numberChecked++;
            }

            if (numberChecked == 0)
            {
                await channel.SendMessageAsync("I couldn't find any payment sessions in this chat!");
                return;
            }

        }

        public static async Task AddStripe(SocketSlashCommand command)
        {
            IUser user = null;
            var stripeId = "";

            foreach (var x in command.Data.Options.FirstOrDefault()?.Options)
            {
                switch (x.Name)
                {
                    case "customer":
                        user = (IUser)x.Value;
                        break;
                    case "reshipper":
                        stripeId = x.Value.ToString();
                        break;
                }
            }

            var successful = Database.AddStripeAccount(user, stripeId);
            var channel = (IMessageChannel) _client.Rest.GetChannelAsync(command.Channel.Id);

            await channel.SendMessageAsync(successful
                ? $":white_check_mark: Successfully added <@{user.Id}> to the Stripe database!"
                : $":x: Could not add <@{user.Id}> to the Stripe database!");
        }
    }
}
