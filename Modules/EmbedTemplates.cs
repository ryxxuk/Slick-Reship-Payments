using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Stripe;

namespace SlickReship_Payments.Modules
{
    class EmbedTemplates
    {
        public static Embed CreatePaymentEmbed(double amount, double transactionFee, string stripeSessionId)
        {
            return new EmbedBuilder()
                .WithAuthor(author =>
                {
                    author.IconUrl = "https://i.imgur.com/DITujMN.png";
                    author.Name = "Slick Reship";
                })
                .WithColor(Color.Blue)
                .WithTitle($"Payment Session Created")
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Service Fee",
                    Value = $"£{amount:0.00}",
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
                    Value = $"£{amount + transactionFee:0.00}",
                    IsInline = true
                })
                .WithFields(new EmbedFieldBuilder
                {
                    Name = "Payment Link",
                    Value =
                        $"[CLICK HERE TO PAY NOW](https://slickrentals.s3.eu-west-2.amazonaws.com/pay.html?sessionid={stripeSessionId}) :white_check_mark:",
                })
                .WithFooter(stripeSessionId)
                .WithCurrentTimestamp()
                .Build();
        }

        public static Embed CreatePaymentCheckEmbed(bool paid, string stripeSessionId)
        {
            return new EmbedBuilder()
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
                 .WithFooter(stripeSessionId)
                 .WithCurrentTimestamp()
                 .Build();
        }
    }
}
