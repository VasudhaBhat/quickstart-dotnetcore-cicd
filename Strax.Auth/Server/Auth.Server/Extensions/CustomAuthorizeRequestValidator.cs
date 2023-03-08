using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Validation;
using System.Threading.Tasks;

namespace FootAI.AuthServer.Extensions
{
    public class CustomAuthorizeRequestValidator : ICustomAuthorizeRequestValidator
    {
        public Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
        {
            var request = context.Result.ValidatedRequest;
            if (string.IsNullOrWhiteSpace(request.Raw["prompted"]))
            {
                request.Raw.Add("prompted", "true");
                request.PromptMode = OidcConstants.PromptModes.Login;
            }
            else if (request.Subject.IsAuthenticated())
            {
                request.PromptMode = OidcConstants.PromptModes.None;
            }
            return Task.CompletedTask;
        }
    }
}
