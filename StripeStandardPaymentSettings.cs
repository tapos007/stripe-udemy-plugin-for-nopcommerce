using Nop.Core.Configuration;
using Nop.Plugin.Payments.StripeStandard.EnumWork;
using Stripe;

namespace Nop.Plugin.Payments.StripeStandard
{
    
    /// <summary>
    /// Represents settings of the Stripe Standard payment plugin
    /// </summary>
    public class StripeStandardPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets test publishable key
        /// </summary>
        public string TestPublishableKey { get; set; }

        /// <summary>
        /// Gets or sets test secret key
        /// </summary>
        public string TestSecretKey { get; set; }
        
        /// <summary>
        /// Gets or sets live publishable key
        /// </summary>
        public string LivePublishableKey { get; set; }

        /// <summary>
        /// Gets or sets live secret key
        /// </summary>
        public string LiveSecretKey { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        
        /// <summary>
        /// Gets or sets payment types
        /// </summary>
        public PaymentType PaymentType { get; set; }

        public string GetSecretKey()
        {
            return UseSandbox ? TestSecretKey : LiveSecretKey;
        }

        public string GetPublishableKey()
        {
            return UseSandbox ? TestPublishableKey : LivePublishableKey;
        }

        public StripeClient GetStripeClient()
        {
            return new  StripeClient(GetSecretKey());
        }
    }
}