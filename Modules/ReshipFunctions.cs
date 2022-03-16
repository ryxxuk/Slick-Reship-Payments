using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickReship_Payments.Functions;

namespace SlickReship_Payments.Modules
{
    public class ReshipFunctions : ModuleBase<SocketCommandContext>
    {
        [Command("reship")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Reship(double reshipFee, IGuildUser customer, IGuildUser reshipper)
        {
            try
            {
                var config = DiscordFunctions.GetConfig();

                var tempMessage = await ReplyAsync("Generating Payment Session...");

                var transactionFee = reshipFee / (1 - Convert.ToDouble(config["stripe_percent_fee"])) - reshipFee;

                transactionFee = Math.Round(transactionFee, 2) + 0;

                var totalCost = reshipFee + transactionFee;

                var stripeId = Database.GetStripeId(reshipper.Id);

                if (stripeId == "")
                {
                    await ReplyAsync($"<@{reshipper.Id}> does not have an associated Stripe account. See ~help for more details.");
                    return;
                }

                var commission = transactionFee + (customer.RoleIds.Any(role => role == 896053430341222420) ? reshipFee * 0.05 : reshipFee * 0.20);

                Console.WriteLine($"{stripeId}, {totalCost}, {commission}");

                var stripeSession = await Functions.Stripe.CreateChargeSessionAsync(
                    $"Reshipping Fee: £{reshipFee}. Trans Fee: £{transactionFee}",
                    totalCost,
                    stripeId,
                    commission);

                var embedBuilder = new EmbedBuilder();
                var embed = embedBuilder
                    .WithAuthor(author =>
                    {
                        author.IconUrl = "https://i.imgur.com/DITujMN.png";
                        author.Name = "Slick Reship";
                    })
                    .WithColor(Color.Blue)
                    .WithTitle($"Payment Session Created")
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Reshipping Fee",
                        Value = $"£{reshipFee:0.00}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Stripe Trans Fee",
                        Value = $"£{transactionFee}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Total Cost",
                        Value = $"£{totalCost:0.00}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Payment Link",
                        Value =
                            $"[CLICK HERE TO PAY NOW](https://slickrentals.s3.eu-west-2.amazonaws.com/pay.html?sessionid={stripeSession.Id}) :white_check_mark:",
                    })
                    .WithFooter(stripeSession.Id)
                    .WithCurrentTimestamp()
                    .Build();


                await ReplyAsync("", embed: embed);
                await tempMessage.DeleteAsync();
                await Context.Message.DeleteAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
                Console.WriteLine(e);
            }
        }


        [Command("deliver")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task DeliveryCost(double deliverCost, IGuildUser customer, IGuildUser reshipper)
        {
            try
            {
                var config = DiscordFunctions.GetConfig();

                var tempMessage = await ReplyAsync("Generating Payment Session...");

                var transactionFee = Math.Round(deliverCost / (1 - Convert.ToDouble(config["stripe_percent_fee"])), 2) - deliverCost;

                var totalCost = deliverCost + transactionFee;

                var stripeId = Database.GetStripeId(reshipper.Id);

                if (stripeId == "")
                {
                    await ReplyAsync($"<@{reshipper.Id}> does not have an associated Stripe account. See ~help for more details.");
                    return;
                }
                
                Console.WriteLine($"{stripeId}, {totalCost}, {transactionFee}");

                var stripeSession = await Functions.Stripe.CreateChargeSessionAsync(
                    $"Reshipping Fee: £{deliverCost}. Trans Fee: £{transactionFee}",
                    totalCost,
                    stripeId,
                    transactionFee);

                var embedBuilder = new EmbedBuilder();
                var embed = embedBuilder
                    .WithAuthor(author =>
                    {
                        author.IconUrl = "https://i.imgur.com/DITujMN.png";
                        author.Name = "Slick Reship";
                    })
                    .WithColor(Color.Blue)
                    .WithTitle($"Payment Session Created")
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Delivery Fee",
                        Value = $"£{deliverCost:0.00}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Stripe Trans Fee",
                        Value = $"£{transactionFee:0.00}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Total Cost",
                        Value = $"£{totalCost:0.00}",
                        IsInline = true
                    })
                    .WithFields(new EmbedFieldBuilder
                    {
                        Name = "Payment Link",
                        Value =
                            $"[CLICK HERE TO PAY NOW](https://slickrentals.s3.eu-west-2.amazonaws.com/pay.html?sessionid={stripeSession.Id}) :white_check_mark:",
                    })
                    .WithFooter(stripeSession.Id)
                    .WithCurrentTimestamp()
                    .Build();


                await ReplyAsync("", embed: embed);
                await tempMessage.DeleteAsync();
                await Context.Message.DeleteAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync("Uh oh... I seem to have broke. <@271005417923018764> Will be here shortly to help. Sorry about any inconvenience");
                Console.WriteLine(e);
            }
        }
    }
}