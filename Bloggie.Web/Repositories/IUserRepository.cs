using Bloggie.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace Bloggie.Web.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<IdentityUser>> GetAll();
        Task<IdentityResult> CreateUserAsync(RegisterViewModel registerViewModel);



        Task GenerateEmailConfirmationTokenAsync(RegisterViewModel registerViewModel);

        Task<IdentityResult> ConfirmEmailAsync(string uid, string token);
    }
}
