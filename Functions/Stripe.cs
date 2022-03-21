using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stripe;
using Stripe.Checkout;

namespace SlickReship_Payments.Functions
{
    public static class Stripe
    {
        public static JObject _config;

        static Stripe()
        {
            var privKey = _config["stripe_private_key"].Value<string>().ToString();

            StripeConfiguration.ApiKey = privKey;
        }
        
        public static async Task<Session> CreateChargeSessionAsync(string note, double price, string destinationStripeId, double applicationFee)
        {
            var capabilityService = new CapabilityService();

            var capability = await capabilityService.GetAsync(destinationStripeId, "card_payments");

            var applicationFeeInPence = Convert.ToInt64(applicationFee * 100);

            var priceInPence = Convert.ToInt64(price * 100);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Name = "Parcel Reshipping Services",
                        Amount = priceInPence,
                        Currency = "gbp",
                        Quantity = 1,
                        Description = note
                    }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = applicationFeeInPence,
                    TransferData = new SessionPaymentIntentDataTransferDataOptions
                    {
                        Destination = destinationStripeId
                    },
                    OnBehalfOf = capability.Status == "inactive" || capability.Status == "unrequested"
                        ? null
                        : destinationStripeId,
                    StatementDescriptor = "Slick Reship",
                    StatementDescriptorSuffix = $"Slick Reship"
                },
                Mode = "payment",
                SuccessUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/success.html",
                CancelUrl = "https://slickrentals.s3.eu-west-2.amazonaws.com/failure.html"
            };
            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public static async Task<bool> CheckIfPaid(string sessionId)
        {
            var service = new SessionService();
            try
            {
                var session = await service.GetAsync(sessionId);
                if (session.PaymentStatus == "paid" || session.PaymentStatus == "no_payment_required") return true;
            }
            catch (Exception)

            {
                // ignored or couldn't find session
            }

            return false;
        }
    }
}