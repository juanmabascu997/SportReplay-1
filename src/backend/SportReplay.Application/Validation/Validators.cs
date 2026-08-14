using FluentValidation;
using SportReplay.Application.Contracts.Auth;
using SportReplay.Application.Contracts.Cameras;
using SportReplay.Application.Contracts.Clubs;
using SportReplay.Application.Contracts.Courts;
using SportReplay.Application.Contracts.Matches;
using SportReplay.Application.Contracts.Payments;
using SportReplay.Application.Contracts.Videos;
using SportReplay.Domain.Enums;

namespace SportReplay.Application.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).Must(r => r is null || UserRoles.All.Contains(r))
            .WithMessage("Role must be Admin, ClubOwner, Operator or Player.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateClubRequestValidator : AbstractValidator<CreateClubRequest>
{
    public CreateClubRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateCourtRequestValidator : AbstractValidator<CreateCourtRequest>
{
    public CreateCourtRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public class CreateCameraRequestValidator : AbstractValidator<CreateCameraRequest>
{
    public CreateCameraRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Fps).InclusiveBetween(1, 120);
        RuleFor(x => x.Bitrate).InclusiveBetween(100, 50000);
        RuleFor(x => x.RtspUrl).NotEmpty().When(x => !x.IsSimulated && x.Protocol == CameraProtocol.Rtsp);
    }
}

public class CreateMatchRequestValidator : AbstractValidator<CreateMatchRequest>
{
    public CreateMatchRequestValidator()
    {
        RuleFor(x => x.CourtId).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
    }
}

public class CreateVideoClipRequestValidator : AbstractValidator<CreateVideoClipRequest>
{
    public CreateVideoClipRequestValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.RecordingId).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
    }
}

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.VideoClipId).NotEmpty();
    }
}

public class CreateVideoRequestRequestValidator : AbstractValidator<CreateVideoRequestRequest>
{
    public CreateVideoRequestRequestValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.VideoClipId).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty().MinimumLength(8);
    }
}
