using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Users;
using SignUpRequest =
    Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpRequest;


namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Extensions;

public static class MappingExtensions
{
    #region To Command

    public static SignUpUseCase.Command ToCommand(
        this SignUpRequest dto) =>
        new(
            dto.FirstName,
            dto.LastName,
            dto.PhoneNumber,
            dto.Dob,
            dto.UserType,
            dto.Email,
            dto.Password);

    #endregion

    #region To Request

    public static KeyCloakCreateUserRequest ToRequest(
        this SignUpUseCase.Command dto)
    {
        var credentials = new KeyCloakCredentials(
            "Password",
            dto.Password,
            false);

        return new KeyCloakCreateUserRequest(
            dto.Email,
            dto.Email,
            dto.FirstName,
            dto.LastName,
            true,
            true,
            [credentials]);
    }

    #endregion
}
