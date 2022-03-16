using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SlickReship_Payments.Functions;

namespace SlickReship_Payments.Modules
{
    public class Utility : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Help()
        {
            var embedBuilder = new EmbedBuilder();

            var embed = embedBuilder
                .WithAuthor(author =>
                {
                    author.IconUrl = "https://i.imgur.com/DITujMN.png";
                    author.Name = "Slick Reship";
                })
                .WithFooter("Slick Reship")
                .WithThumbnailUrl("")
                .WithColor(Color.Blue)
                .WithTitle("Reship Reship Bot Command Help")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Commands",
                    Value = "**~reship** {Reship fee in GBP} {@customer} {@reshipper}\n**~addstripe** {@user} {stripeId}\n**~deliver** {deliveryCost in £} @customer @reshipper\n**~check** - Checks for payment"
                })
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync("", false, embed);
        }
    }
}