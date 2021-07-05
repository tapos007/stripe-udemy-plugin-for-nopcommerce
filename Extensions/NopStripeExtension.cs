using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Plugin.Payments.StripeStandard.EnumWork;
using Nop.Services.Payments;
using Stripe;

namespace Nop.Plugin.Payments.StripeStandard.Extensions
{
    public static class NopStripeExtension
    {
        public static async Task<PaymentIntent> CompletePayment(this ProcessPaymentRequest processPaymentRequest,
            StripeStandardPaymentSettings stripeStandardPaymentSettings)
        {

            var paymentMethod = await GetPaymentMethodResponse(processPaymentRequest,stripeStandardPaymentSettings);
            var paymentIndent = await GetPaymentIndentResponse(paymentMethod,stripeStandardPaymentSettings,processPaymentRequest);

            return paymentIndent;
        }
        
        
        public static async Task<PaymentIntent> CreateCapture(this CapturePaymentRequest capturePaymentRequest,
            StripeStandardPaymentSettings stripeStandardPaymentSettings)
        {

            // To create a requires_capture PaymentIntent, see our guide at: https://stripe.com/docs/payments/capture-later
            var service = new PaymentIntentService(stripeStandardPaymentSettings.GetStripeClient());
            var chargeId = capturePaymentRequest.Order.AuthorizationTransactionId;
                
           return await service.CaptureAsync(chargeId);
        }

        private static async Task<PaymentIntent> GetPaymentIndentResponse(PaymentMethod paymentMethod,
            StripeStandardPaymentSettings stripeStandardPaymentSettings, ProcessPaymentRequest processPaymentRequest)
        {
            
            var service = new PaymentIntentService(stripeStandardPaymentSettings.GetStripeClient());
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long) (processPaymentRequest.OrderTotal* 100m),
                Currency = "usd",
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                PaymentMethod = paymentMethod.Id,
                ConfirmationMethod = "manual",
                CaptureMethod = stripeStandardPaymentSettings.PaymentType == PaymentType.Authorize ? "manual":"automatic",
                Confirm = true
            };
            
           return await service.CreateAsync(options);
        }

        private static async Task<PaymentMethod> GetPaymentMethodResponse(ProcessPaymentRequest processPaymentRequest,
            StripeStandardPaymentSettings stripeStandardPaymentSettings)
        {
            
            var service = new PaymentMethodService(stripeStandardPaymentSettings.GetStripeClient());
            var options = new PaymentMethodCreateOptions
            {
                Type = "card",
                Card = new PaymentMethodCardOptions
                {
                    Number = processPaymentRequest.CreditCardNumber,
                    ExpMonth = processPaymentRequest.CreditCardExpireMonth,
                    ExpYear = processPaymentRequest.CreditCardExpireYear,
                    Cvc = processPaymentRequest.CreditCardCvv2,
                },
                
            };
            
            return await  service.CreateAsync(options);
        }
    }
}