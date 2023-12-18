using Bloggie.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Bloggie.Web.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<IdentityUser>> GetAll();
        Task<IdentityResult> CreateUserAsync(RegisterViewModel registerViewModel);

        Task GenerateEmailConfrimationTokenAsync(IdentityUser identityUser);
        Task<IdentityUser> GetUserByEmailAsync(string email);

        Task GenerateForgotPasswordTokenAsync(IdentityUser identityUser);
        Task<IdentityResult> ConfirmEmailAsync(string uid, string token);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordModel model);

    }
}
