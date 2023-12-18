using Bloggie.Web.Models.ViewModels;

namespace Bloggie.Web.Repositories
{
    public interface IEmailService
    {
        Task SendTestEmail(UserEmailOptions userEmailOptions);
        Task SendEmailForEmailConfirmation(UserEmailOptions userEmailOptions);
        

    }
}