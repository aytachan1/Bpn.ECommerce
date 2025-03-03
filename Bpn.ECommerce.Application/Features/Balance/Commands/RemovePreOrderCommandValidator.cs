using FluentValidation;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class RemovePreOrderCommandValidator : AbstractValidator<RemovePreOrderCommand>
    {
        public RemovePreOrderCommandValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("OrderId is required.")
                .Length(1, 50).WithMessage("OrderId must be between 1 and 50 characters.");
        }
    }
}