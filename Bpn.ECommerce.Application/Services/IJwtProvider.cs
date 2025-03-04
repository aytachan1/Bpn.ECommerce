using Bpn.ECommerce.Application.Features.Auth.Login;
using Bpn.ECommerce.Domain.Entities.User;

namespace Bpn.ECommerce.Application.Services
{
    public interface IJwtProvider
    {
        Task<LoginCommandResponse> CreateToken(AppUser user);
    }
}
