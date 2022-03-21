using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SlickReship_Payments.Functions;
using Embed = Discord.Embed;

namespace SlickReship_Payments.Modules
{
    class SlashCommands
    {
        public static DiscordSocketClient _client;
        public static JObject _config;

        public static async Task Charge(SocketSlashCommand command)
        {
            var client = await _client.Rest.GetGuildUserAsync(893087963582464030, command.User.Id);
            if (!client.RoleIds.Contains<ulong>(894629272172507166) && !client.RoleIds.Contains<ulong>(895710007973249055))
            {
                await command.RespondAsync("You haven't got permission for this!", ephemeral: true);
            }

            IGuildUser reshipper = null;
            IGuildUser customer = null;
            var deliveryCost = false;
            double amount = 0;

            foreach (var x in command.Data.Options)
            {
                switch (x.Name)
                {
                    case "customer":
                        customer = (IGuildUser) x.Value;
                        break;
                    case "reshipper":
                        reshipper = (IGuildUser) x.Value;
                        break;
                    case "amount":
                        amount = (double) x.Value;
                        break;
                    case "deliverycost":
                        deliveryCost = (bool) x.Value;
                        break;
                }
            }

            var stripeId = Database.GetStripeId(reshipper.Id);

            if (stripeId == "")
            {
                await command.RespondAsync($"<@{reshipper.Id}> does not have an associated Stripe account. Use /addstripe to add their account.");
                return;
            }
            
            var transactionFee = amount / (1 - _config["stripe_percent_fee"].Value<double>()) - amount;
            transactionFee = Math.Round(transactionFee + 0.2, 2) ;

            var totalCost = Math.Round(amount + transactionFee, 2);

            var applicationFee = customer.RoleIds.Any(role => role == 896053430341222420) || deliveryCost ? transactionFee : amount * _config["non_premium_commission"].Value<double>() + transactionFee;
            var stripeSession = await Functions.Stripe.CreateChargeSessionAsync(
                $"Reshipping Fee: £{amount}. Trans Fee: £{transactionFee}",
                totalCost,
                stripeId,
                applicationFee);

            var embed = EmbedTemplates.CreatePaymentEmbed(amount, transactionFee, stripeSession.Id);

            await command.RespondAsync("", embed: embed);

            Console.WriteLine($"Created new payment link for {stripeId}, total cost:{totalCost}, fee:{applicationFee}");
        }

        public static async Task Check(SocketSlashCommand command)
        {
            var client = await _client.Rest.GetGuildUserAsync(893087963582464030, command.User.Id);
            if (!client.RoleIds.Contains<ulong>(894629272172507166) && !client.RoleIds.Contains<ulong>(895710007973249055))
            {
                await command.RespondAsync("You haven't got permission for this!", ephemeral: true);
            }

            var numberChecked = 0;

            var messages = command.Channel.GetMessagesAsync().Flatten();

            var embeds = new List<Embed>();

            await foreach (var message in messages)
            {
                if (message.Author.Id != _client.CurrentUser.Id) continue;
                if (message.Embeds.Count != 1) continue;
                if (message.Embeds.First().Title != "Payment Session Created") continue;
                if (message.Reactions.ContainsKey(new Emoji("✅"))) continue;

                var stripeSessionId = message.Embeds.First().Footer.ToString();

                var paid = await Functions.Stripe.CheckIfPaid(stripeSessionId);

                embeds.Add(EmbedTemplates.CreatePaymentCheckEmbed(paid, stripeSessionId));


                if (paid)
                {
                    await message.AddReactionAsync(new Emoji("✅"));
                }

                numberChecked++;
            }

            if (numberChecked == 0)
            {
                await command.RespondAsync("I couldn't find any payment sessions in this chat!");
            }
            else
            {
                await command.RespondAsync("", embeds: embeds.ToArray());
            }

        }

        public static async Task AddStripe(SocketSlashCommand command)
        {
            var client = await _client.Rest.GetGuildUserAsync(893087963582464030, command.User.Id);
            if (!client.RoleIds.Contains<ulong>(894629272172507166) && !client.RoleIds.Contains<ulong>(895710007973249055))
            {
                await command.RespondAsync("You haven't got permission for this!", ephemeral: true);
            }

            await command.RespondAsync("Command Executed", ephemeral:true);
            IUser user = null;
            var stripeId = "";

            foreach (var x in command.Data.Options)
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
