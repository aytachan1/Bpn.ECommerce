using MediatR;
using Bpn.ECommerce.Domain.Generic.Result;

namespace Bpn.ECommerce.Application.Features.Auth.Login
{
    public sealed record LoginCommand(
        string EmailOrUserName,
        string Password) : IRequest<Result<LoginCommandResponse>>;
}
