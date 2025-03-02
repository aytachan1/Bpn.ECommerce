using Bpn.ECommerce.Application.Features.Auth.Login;
using Bpn.ECommerce.Domain.Entities;

namespace Bpn.ECommerce.Application.Services
{
    public interface IJwtProvider
    {
        Task<LoginCommandResponse> CreateToken(AppUser user);
    }
}
