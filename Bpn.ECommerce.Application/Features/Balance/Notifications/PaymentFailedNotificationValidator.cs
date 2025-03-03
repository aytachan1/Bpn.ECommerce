﻿using FluentValidation;

namespace Bpn.ECommerce.Application.Features.Balance.Notifications
{
    public class PaymentFailedNotificationValidator : AbstractValidator<PaymentFailedNotification>
    {
        public PaymentFailedNotificationValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty().WithMessage("OrderId is required.")
                .Length(1, 50).WithMessage("OrderId must be between 1 and 50 characters.");
        }
    }
}