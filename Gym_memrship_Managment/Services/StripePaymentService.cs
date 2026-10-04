using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gym_memrship_Managment.Models;
using Microsoft.Extensions.Configuration;

namespace Gym_memrship_Managment.Services
{
    public class StripePaymentService
    {
        private readonly IConfiguration _config;
        public StripePaymentService(IConfiguration config)
        {
            _config = config;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"] ?? "sk_test_4eC39HqLyjWDarjtT1zdp7dc";
        }

        // Existing: Pay dues on an existing membership
        public async Task<Session> CreateCheckoutSessionAsync(Membership membership, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(membership.DueAmount * 100),
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Gym Membership Due - " + membership.Plan?.PlanName,
                            },
                        },
                        Quantity = 1,
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = membership.MembershipId.ToString(),
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        // NEW: Purchase a brand-new membership plan
        public async Task<Session> CreatePlanCheckoutSessionAsync(
            MembershipPlan plan, MemberProfile member, decimal totalAmount, string successUrl, string cancelUrl)
        {
            // Build line items
            var lineItems = new List<SessionLineItemOptions>();

            // Plan price
            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(plan.Price * 100),
                    Currency = "inr",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{plan.PlanName} Membership",
                        Description = $"{plan.DurationInDays} days - {plan.Description}"
                    },
                },
                Quantity = 1,
            });

            // Registration fee as separate line item if applicable
            if (plan.RegistrationFee > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(plan.RegistrationFee * 100),
                        Currency = "inr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "One-time Registration Fee",
                        },
                    },
                    Quantity = 1,
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = member.Email,
                ClientReferenceId = member.MemberId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "type", "new" },
                    { "planId", plan.PlanId.ToString() },
                    { "memberId", member.MemberId.ToString() }
                }
            };

            var service = new SessionService();
                return await service.CreateAsync(options);
        }

        // NEW: Online renewal of an existing membership
        public async Task<Session> CreateRenewalCheckoutSessionAsync(
            Membership membership, decimal renewalAmount, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(renewalAmount * 100),
                            Currency = "inr",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Renew {membership.Plan?.PlanName}",
                                Description = $"Membership renewal for another {membership.Plan?.DurationInDays} days",
                            },
                        },
                        Quantity = 1,
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = membership.MembershipId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "type", "renewal" },
                    { "membershipId", membership.MembershipId.ToString() },
                    { "planId", membership.PlanId.ToString() }
                }
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }
    }
}
