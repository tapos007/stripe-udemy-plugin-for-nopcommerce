using Nop.Plugin.Payments.StripeStandard.Models;
using Nop.Web.Framework.Validators;
using FluentValidation;

namespace Nop.Plugin.Payments.StripeStandard.Validators
{
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        public ConfigurationValidator()
        {

            RuleFor(model => model.Title)
                .NotEmpty().WithMessage("Title not empty");
            
            
            RuleFor(model => model.TestPublishableKey)
                .NotEmpty().WithMessage("Test Publishable key required")
                .When(model =>model.UseSandbox);
            
            RuleFor(model => model.TestSecretKey)
                .NotEmpty().WithMessage("Test Secret  key required")
                .When(model =>model.UseSandbox);
            
            
            RuleFor(model => model.LivePublishableKey)
                .NotEmpty().WithMessage("Live Publishable key required")
                .When(model => !model.UseSandbox);
            
            RuleFor(model => model.LiveSecretKey)
                .NotEmpty().WithMessage("Live Secret  key required")
                .When(model => !model.UseSandbox);
            
        }
    }
}