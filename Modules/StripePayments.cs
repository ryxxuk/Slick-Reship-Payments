using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SlickReship_Payments.Modules
{
    public class StripePayments : ModuleBase<SocketCommandContext>
    {
        [Command("check")]
        public async Task Check()
        {
            try
            {
                var numberChecked = 0;

                var messages = Context.Channel.GetMessagesAsync().Flatten();

                await foreach (var message in messages)
                {
                    if (message.Author.Id != Context.Client.CurrentUser.Id) continue;
                    if (message.Embeds.Count != 1) continue;
                    if (message.Embeds.First().Title != "Payment Session Created") continue;
                    if (message.Reactions.ContainsKey(new Emoji("✅"))) continue;

                    var paid = await Functions.Stripe.CheckIfPaid(message.Embeds.First().Footer.ToString());

                    var embedBuilder = new EmbedBuilder();
                    var embed = embedBuilder
                        .WithAuthor(author =>
                        {
                            author.IconUrl =
                                "https://i.imgur.com/DITujMN.png";
                            author.Name = "Slick Reship";
                        })
                        .WithColor(paid ? Color.Green : Color.Red)
                        .WithTitle(paid
                            ? $"Reshipping Fee Paid! :white_check_mark:"
                            : $"Reshipping Fee has not been paid yet! :no_entry_sign:")
                        .WithFooter($"{message.Embeds.First().Footer}")
                        .WithCurrentTimestamp()
                        .Build();

                    numberChecked++;
                    await ReplyAsync("", embed: embed);

                    if (paid)
                    {
                        await message.AddReactionAsync(new Emoji("✅"));
                    }
                }

                if (numberChecked == 0)
                {
                    await ReplyAsync(
                        "I couldn't find any payment sessions in this chat!");
                    return;
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(
                    "Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
                Console.WriteLine(e);
            }
        }

        [Command("addstripe")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task AddStripe(IGuildUser user, string stripeId)
        {
            var successful = Functions.Database.AddStripeAccount(user, stripeId);

            await ReplyAsync(successful
                ? $":white_check_mark: Successfully added <@{user.Id}> to the Stripe database!"
                : $":x: Could not add <@{user.Id}> to the Stripe database!");
        }
    }
}