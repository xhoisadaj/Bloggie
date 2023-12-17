using Bloggie.Models.ViewModels;

namespace Bloggie.Repositories
{
    public interface IEmailService
    {
        Task SendTestEmail(UserEmailOptions userEmailOptions);
        Task SendEmailForEmailConfirmation(UserEmailOptions userEmailOptions);
        

    }
}