using Bpn.ECommerce.Application.Features.Balance.Commands;
using Bpn.ECommerce.Domain.Entities;
using FluentValidation;
namespace Bpn.ECommerce.Application.Behaviors.IncomingValidations
{

    public class OrderItemValidator : AbstractValidator<OrderItem>
    {
        public OrderItemValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required.");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        }
    }
}
