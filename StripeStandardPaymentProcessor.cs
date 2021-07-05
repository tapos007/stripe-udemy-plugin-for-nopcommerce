using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MySqlX.XDevAPI.Common;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Infrastructure;
using Nop.Plugin.Payments.StripeStandard.EnumWork;
using Nop.Plugin.Payments.StripeStandard.Extensions;
using Nop.Plugin.Payments.StripeStandard.Models;
using Nop.Plugin.Payments.StripeStandard.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.StripeStandard
{
    public class StripeStandardPaymentProcessor : BasePlugin, IPaymentMethod
    {
        
        #region Fields
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly StripeStandardPaymentSettings _stripeStandardPaymentSettings;
        private readonly IPaymentService _paymentService;

        #endregion

        #region Ctor
        public StripeStandardPaymentProcessor(ISettingService settingService,
            ILocalizationService localizationService,
            IWebHelper webHelper,
            StripeStandardPaymentSettings stripeStandardPaymentSettings,
            IPaymentService paymentService
            )
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _stripeStandardPaymentSettings = stripeStandardPaymentSettings;
            _paymentService = paymentService;
        }
        
        #endregion

        #region Methods
        
        
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentStripeStandard/Configure";
        }
        
        
        
        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new StripeStandardPaymentSettings()
            {
                UseSandbox = true,
                Title = "Credit Card (Stripe)",
                AdditionalFee = 0,
                PaymentType = PaymentType.Authorize
            });

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.StripeStandard.Fields.AdditionalFee"] = "Additional fee",
                ["Plugins.Payments.StripeStandard.Fields.AdditionalFee.Hint"] = "Enter additional fee to charge your customers.",
                ["Plugins.Payments.StripeStandard.Fields.AdditionalFeePercentage"] = "Additional fee. Use percentage",
                ["Plugins.Payments.StripeStandard.Fields.AdditionalFeePercentage.Hint"] = "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.",
                ["Plugins.Payments.StripeStandard.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.Payments.StripeStandard.Fields.UseSandbox.Hint"] = "Check to enable Sandbox (testing environment).",
                ["Plugins.Payments.StripeStandard.Fields.Title"] = "Title",
                ["Plugins.Payments.StripeStandard.Fields.Title.Hint"] = "Specify your title.",
                ["Plugins.Payments.StripeStandard.Fields.TestPublishableKey"] = "Test Publishable Key",
                ["Plugins.Payments.StripeStandard.Fields.TestPublishableKey.Hint"] = "Specify your test publishable key.",
                ["Plugins.Payments.StripeStandard.Fields.TestSecretKey"] = "Test Secret Key",
                ["Plugins.Payments.StripeStandard.Fields.TestSecretKey.Hint"] = "Specify your test secret key.",
                ["Plugins.Payments.StripeStandard.Fields.LivePublishableKey"] = "Live Publishable Key",
                ["Plugins.Payments.StripeStandard.Fields.LivePublishableKey.Hint"] = "Specify your live publishable key.",
                ["Plugins.Payments.StripeStandard.Fields.LiveSecretKey"] = "Live Secret Key",
                ["Plugins.Payments.StripeStandard.Fields.LiveSecretKey.Hint"] = "Specify your live secret key.",
                ["Plugins.Payments.StripeStandard.Fields.PaymentType"] = "Payment Type",
                ["Plugins.Payments.StripeStandard.Fields.PaymentType.Hint"] = "Specify your payment type",
                ["Plugins.Payments.StripeStandard.Instructions"] = @"
                    <p>
                        For plugin configuration follow these steps:<br />
                        <br />
                        1. You will need a Stripe Merchant account. If you don't already have one, you can sign up here: <a href=""https://dashboard.stripe.com/register"" target=""_blank"">https://dashboard.stripe.com/register</a><br />
                        <em>Important: Your merchant account must be approved by Stripe prior to you be able to cash out payments.</em><br />
                        2. Sign in to your Stripe Developer Portal at <a href=""https://dashboard.stripe.com/login"" target=""_blank"">https://dashboard.stripe.com/login</a>; use the same sign in credentials as your merchant account.<br />
                        3. Use the API keys provided at <a href=""https://dashboard.stripe.com/account/apikeys"" target=""_blank"">https://dashboard.stripe.com/account/apikeys</a> to configure the account.
                        <br />
                    </p>",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<StripeStandardPaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.StripeStandard");

            await base.UninstallAsync();
        }
        
        

        
        
        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {

            var result = new ProcessPaymentResult()
            {
                NewPaymentStatus = _stripeStandardPaymentSettings.PaymentType == PaymentType.Authorize
                    ? PaymentStatus.Authorized
                    : PaymentStatus.Paid
            };

            var currentStore = await EngineContext.Current.Resolve<Nop.Core.IStoreContext>().GetCurrentStoreAsync();

            var paymentResponse = await processPaymentRequest.CompletePayment(_stripeStandardPaymentSettings);

            if (paymentResponse.Status != "succeeded" && paymentResponse.Status != "requires_capture")
                    throw new NopException("some error");


            var transactionResult = $"Transaction was processed by using stripe, Status is {paymentResponse.Status}";
            if (_stripeStandardPaymentSettings.PaymentType == PaymentType.Capture)
            {
                result.CaptureTransactionId = paymentResponse.Id;
                result.CaptureTransactionResult = transactionResult;
            }else if (_stripeStandardPaymentSettings.PaymentType == PaymentType.Authorize)
            {
                result.AuthorizationTransactionId = paymentResponse.Id;
                result.AuthorizationTransactionResult = transactionResult;
            }

            return result;
        }

        
        
        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            return Task.CompletedTask;
        }

        
        
        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {


            bool  dataMissing = string.IsNullOrEmpty(_stripeStandardPaymentSettings.GetPublishableKey()) ||
                                string.IsNullOrEmpty(_stripeStandardPaymentSettings.GetSecretKey());

            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(dataMissing);
        }

        
        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public async Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return await _paymentService.CalculateAdditionalFeeAsync(cart,
                _stripeStandardPaymentSettings.AdditionalFee, _stripeStandardPaymentSettings.AdditionalFeePercentage);
        }

        
        
        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            
            // need to implementation
            if (capturePaymentRequest == null)
            {
                throw new ArgumentNullException(nameof(capturePaymentRequest));
            }

            var capture = await capturePaymentRequest.CreateCapture(_stripeStandardPaymentSettings);

            if (capture.Status == "succeeded")
            {
                return new CapturePaymentResult()
                {
                    NewPaymentStatus = PaymentStatus.Paid, CaptureTransactionId = capture.Id
                };
            }

            return new CapturePaymentResult()
            {
                Errors =
                    new List<string>(new[] {$"An error occured attempting to capture charge {capture.Id}."}),
                NewPaymentStatus = PaymentStatus.Authorized,
                CaptureTransactionId = capture.Id
            };
        }

        
        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult { Errors = new[] { "Refund method not supported" } });
        }

        
        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        
        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        
        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        
        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return Task.FromResult(false);

            return Task.FromResult(true);
        }

        
        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return Task.FromResult<IList<string>>(warnings);
        }

        
        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest
            {
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            });
        }

        
        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentStripeStandard";
        }

        
        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await Task.FromResult(_stripeStandardPaymentSettings.Title);
        }

        #endregion
        
        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;


        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => true;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        #endregion
    }
}