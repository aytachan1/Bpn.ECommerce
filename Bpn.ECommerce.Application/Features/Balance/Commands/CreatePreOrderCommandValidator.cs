using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class CreatePreOrderCommandValidator : AbstractValidator<CreatePreOrderCommand>
    {
        public CreatePreOrderCommandValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
        }
    }
}
