using Bpn.ECommerce.Application.Behaviors;
using Bpn.ECommerce.Application.Behaviors.IncomingValidations;
using Bpn.ECommerce.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Bpn.ECommerce.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(DependencyInjection).Assembly);

            services.AddMediatR(conf =>
            {
                conf.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
                conf.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            return services;
        }
    }
}
