using Bloggie.Web.Models.ViewModels;
using Bloggie.Web.Repositories;
using Bloggie.Web.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Web.Helpers;

namespace Bloggie.Web.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthDbContext authDbContext;
        private readonly UserManager<IdentityUser> userManager;
        private readonly IEmailService emailService;
        private readonly IConfiguration configuration;

        public UserRepository(AuthDbContext authDbContext, UserManager<IdentityUser> userManager, IEmailService emailService, IConfiguration configuration)
        {
            this.authDbContext = authDbContext;
            this.userManager = userManager;
            this.emailService = emailService;
            this.configuration = configuration;
        }

        public async Task<IdentityResult> CreateUserAsync(RegisterViewModel registerViewModel)
        {
            var identityUser = new IdentityUser
            {
                UserName = registerViewModel.Username,
                Email = registerViewModel.Email
            };

            var identityResult = await userManager.CreateAsync(identityUser, registerViewModel.Password);

            if (identityResult.Succeeded)
            {
                // Assign this user the "User" role
                var roleIdentityResult = await userManager.AddToRoleAsync(identityUser, "User");

                if (roleIdentityResult.Succeeded)
                {
                    await GenerateEmailConfrimationTokenAsync(identityUser);

                    // Show success notification
                    // return RedirectToAction("ConfirmEmail", new { email = userModel.Email });

                    // return View(identityUser);
                }

                return identityResult;
            }

            // Return a default value or throw an exception, depending on your requirements
            return identityResult;
        }

        public async Task GenerateEmailConfrimationTokenAsync(IdentityUser identityUser)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser);

            if (!string.IsNullOrEmpty(token))
            {
                await SendEmailConfirmationEmail(identityUser, token);
            }

        }
        public async Task<IdentityUser> GetUserByEmailAsync(string email)
        {
           // return await userManager.FindByEmailAsync(Email);
            return await userManager.FindByEmailAsync(email);
        }

        private async Task SendEmailConfirmationEmail(IdentityUser user, string token)
        {

            string appDomain = configuration.GetSection("Application:AppDomain").Value;
            string confirmationLink = configuration.GetSection("Application:EmailConfirmation").Value;
            UserEmailOptions options = new UserEmailOptions
            {
                ToEmails = new List<string>() { user.Email },
                PlaceHolders = new List<KeyValuePair<string, string>>()
                {
                new KeyValuePair<string, string>("{{UserName}}",user.UserName),
                new KeyValuePair<string, string>("{{Link}}", string.Format(appDomain + confirmationLink, user.Id,token))
                }
            };


            await emailService.SendEmailForEmailConfirmation(options);
        }
   

        public async Task<IdentityResult> ConfirmEmailAsync(string uid, string token)
        {
            // Find the user by their user ID (uid) using the user manager.
            var user = await userManager.FindByIdAsync(uid);

            // Confirm the user's email using the user manager and the provided token.
            return await userManager.ConfirmEmailAsync(user, token);
        }

        public async Task<IEnumerable<IdentityUser>> GetAll()
        {
            var users = await authDbContext.Users.ToListAsync();

            var superAdminUser = await authDbContext.Users
                .FirstOrDefaultAsync(x => x.Email == "superadmin@bloggie.com");

            if (superAdminUser is not null)
            {
                users.Remove(superAdminUser);
            }

            return users;
        }

   
    }
}
