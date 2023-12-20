
using Bloggie.Web.Models.ViewModels;
using Bloggie.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bloggie.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IEmailService emailService;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;

        public AccountController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailService emailService,
            IUserRepository userRepository,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailService = emailService;
            this.userRepository = userRepository;
            this.configuration = configuration;
        }


        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {

            if (ModelState.IsValid)
            {
                // write your code
                var result = await userRepository.CreateUserAsync(registerViewModel);
                if (!result.Succeeded)
                {
                    foreach (var errorMessage in result.Errors)
                    {
                        ModelState.AddModelError("", errorMessage.Description);
                    }

                    return View(registerViewModel);
                }

               ModelState.Clear();
               return RedirectToAction("ConfirmEmail", new {email= registerViewModel.Email});

               
            }
            //return RedirectToAction("ConfirmEmail");

            return View(registerViewModel);
         
        }


        [HttpGet]
        public IActionResult Login(string ReturnUrl)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = ReturnUrl
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Login failed. Please check your credentials.";
                return View();
            }

            var signInResult = await signInManager.PasswordSignInAsync(loginViewModel.Username,
                loginViewModel.Password, false, false);

            if (signInResult != null && signInResult.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(loginViewModel.ReturnUrl))
                {
                    return Redirect(loginViewModel.ReturnUrl);
                }

                // Set success message in TempData
                TempData["SuccessMessage"] = "Login successful!";

                return RedirectToAction("Index", "Home");
            }
            else if (signInResult.IsNotAllowed)
            {
                ModelState.AddModelError("", "Not allowed to login");
            }
            else if (signInResult.IsLockedOut)
            {
                ModelState.AddModelError("", "Account blocked. Try after some time.");
            }
            else
            {
                ModelState.AddModelError("", "Invalid credentials");
            }
            TempData["ErrorMessage"] = "Login failed. Please check your credentials.";
            // Show errors
            return View();
        }




     



        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Logout successful!";
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            // Retrieve the user's current information
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                // Handle the case where the user is not found
                return NotFound();
            }

            var model = new UpdateProfileViewModel
            {
                // Populate the view model with the current user's information
                Username = user.UserName,
                Email = user.Email
                // Add other properties as needed
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            // Update non-password information
            user.UserName = model.Username;
            user.Email = model.Email;

            // Update the password if new password is provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var changePasswordResult = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Show success notification
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }




        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string uid, string token, string email)
        {
            EmailConfirmModel model = new EmailConfirmModel
            {
                Email = email


            };
            // Now you can use uid and token in your application logic
            // For example, you can verify the email confirmation or process the user account
            if (!string.IsNullOrEmpty(uid) && !string.IsNullOrEmpty(token))
            {
                token = token.Replace(' ', '+');

                // Retrieve the IdentityUser instance based on uid
                var user = await userManager.FindByIdAsync(uid);

                if (user != null)
                {
                    var result = await userManager.ConfirmEmailAsync(user, token);

                    // Process the result if needed
                    if (result.Succeeded)
                    {
                        // Do something
                      //  ViewBag.IsSuccess = true;
                        model.EmailVerified = true;
                    }
                }
            }
       

                return View(model);
            //return Content($"Email Confirmation: uid={uid}, token={token}");
        }

   

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(EmailConfirmModel model)
        {

            var user = await userRepository.GetUserByEmailAsync(model.Email);
            if (user != null)
            {
                if (user.EmailConfirmed)
                {
                    model.EmailVerified = true;
                    return View(model);
                }
                await userRepository.GenerateEmailConfrimationTokenAsync(user);
                model.EmailSent = true;
                //ModelState.AddModelError();
            }
            else
            {
                ModelState.AddModelError("", "Something went wrong.");
            }
            return View(model);
            //return Content($"Email Confirmation: uid={uid}, token={token}");
        }


        [AllowAnonymous , HttpGet("forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [AllowAnonymous, HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userRepository.GetUserByEmailAsync(model.Email);
                if (user != null)
                {
                    await userRepository.GenerateForgotPasswordTokenAsync(user);
                }

                ModelState.Clear();
                model.EmailSent = true;
            }
            return View(model);
        }

        [AllowAnonymous, HttpGet("reset-password")]
        public IActionResult ResetPassword(string uid, string token)
        {
            ResetPasswordModel resetPasswordModel = new ResetPasswordModel
            {
                Token=token,
                UserId=uid
            };
            return View(resetPasswordModel);
        }
        [AllowAnonymous, HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                model.Token = model.Token.Replace(' ','+');
                var result = await userRepository.ResetPasswordAsync(model);
                if (result.Succeeded)
                {
                    ModelState.Clear();
                    model.IsSuccess = true;
                    return View(model);
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            
            }
            return View(model);
        }
    }
}
