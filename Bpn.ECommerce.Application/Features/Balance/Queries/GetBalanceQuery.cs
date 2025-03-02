using Bpn.ECommerce.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Queries
{
    public class GetBalanceQuery : IRequest<BalanceResponse>
    {
    }
}
