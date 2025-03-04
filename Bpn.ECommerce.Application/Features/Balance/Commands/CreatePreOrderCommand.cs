using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class CreatePreOrderCommand : IRequest<Result<PreOrderResponse>>
    {
        public decimal Amount { get; set; }
        public string? OrderId { get; set; }
    }
}
