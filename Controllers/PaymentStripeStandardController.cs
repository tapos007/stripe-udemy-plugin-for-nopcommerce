using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Payments.StripeStandard.EnumWork;
using Nop.Plugin.Payments.StripeStandard.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.StripeStandard.Controllers
{
    public class PaymentStripeStandardController : BasePaymentController
    {
        

        #region Fields
        
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;

        #endregion
        
        
        #region Ctor
        public PaymentStripeStandardController(IPermissionService permissionService,
            IStoreContext storeContext,
            ISettingService settingService,
            INotificationService notificationService,
            ILocalizationService localizationService)
        {
            _permissionService = permissionService;
            _storeContext = storeContext;
            _settingService = settingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
        }
        
        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var stripeStandardPaymentSettings = await _settingService.LoadSettingAsync<StripeStandardPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = stripeStandardPaymentSettings.UseSandbox,
                Title = stripeStandardPaymentSettings.Title,
                TestPublishableKey = stripeStandardPaymentSettings.TestPublishableKey,
                TestSecretKey = stripeStandardPaymentSettings.TestSecretKey,
                LivePublishableKey = stripeStandardPaymentSettings.LivePublishableKey,
                LiveSecretKey = stripeStandardPaymentSettings.LiveSecretKey,
                AdditionalFee = stripeStandardPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = stripeStandardPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope,
                PaymentTypeId = (int) stripeStandardPaymentSettings.PaymentType
            };

            if (storeScope > 0)
            {

                model.UseSandbox_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.UseSandbox,
                        storeScope);
                model.Title_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.Title, storeScope);
                model.TestPublishableKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.TestPublishableKey,
                        storeScope);
                model.TestSecretKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.TestSecretKey,
                        storeScope);
                model.LivePublishableKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.LivePublishableKey,
                        storeScope);
                model.LiveSecretKey_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.LiveSecretKey,
                        storeScope);
                model.AdditionalFee_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.AdditionalFee,
                        storeScope);
                model.AdditionalFeePercentage_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings,
                        x => x.AdditionalFeePercentage, storeScope);
                model.PaymentTypeId_OverrideForStore =
                    await _settingService.SettingExistsAsync(stripeStandardPaymentSettings, x => x.PaymentType,
                        storeScope);
            }

            model.PaymentTypes = await (await PaymentType.Authorize.ToSelectListAsync(false))
                .Select(item => new SelectListItem(item.Text, item.Value)).ToListAsync();

            return View("~/Plugins/Payments.StripeStandard/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var payPalStandardPaymentSettings = await _settingService.LoadSettingAsync<StripeStandardPaymentSettings>(storeScope);

            //save settings
            payPalStandardPaymentSettings.UseSandbox = model.UseSandbox;
            payPalStandardPaymentSettings.Title = model.Title;
            payPalStandardPaymentSettings.TestPublishableKey = model.TestPublishableKey;
            payPalStandardPaymentSettings.TestSecretKey = model.TestSecretKey;
            payPalStandardPaymentSettings.LivePublishableKey = model.LivePublishableKey;
            payPalStandardPaymentSettings.LiveSecretKey = model.LiveSecretKey;
            payPalStandardPaymentSettings.PaymentType =  (PaymentType) model.PaymentTypeId;
            payPalStandardPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalStandardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.Title, model.Title_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.TestPublishableKey, model.TestPublishableKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.TestSecretKey, model.TestSecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.LivePublishableKey, model.LivePublishableKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.LiveSecretKey, model.LiveSecretKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.PaymentType, model.PaymentTypeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }
        
        #endregion
    }
}