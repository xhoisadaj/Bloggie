
using Bloggie.Web.Models;
using Bloggie.Web.Models.ViewModels;
using Bloggie.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;

namespace Bloggie.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogPostRepository blogPostRepository;
        private readonly ITagRepository tagRepository;
        private readonly IEmailService emailService;

        public HomeController(ILogger<HomeController> logger,
            IBlogPostRepository blogPostRepository,
            ITagRepository tagRepository,
            IEmailService emailService
            )
        {
            _logger = logger;
            this.blogPostRepository = blogPostRepository;
            this.tagRepository = tagRepository;
            this.emailService = emailService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            //UserEmailOptions options = new UserEmailOptions
            //{
            //    ToEmails=new List<string>() { "xhoisadaj@gmail.com"},
            //    PlaceHolders=  new List<KeyValuePair<string, string>>() { 
            //    new KeyValuePair<string, string>("{{UserName}}","Xhoi")}
            //};

         
            //await emailService.SendTestEmail(options);

            // getting all blogs
            var blogPosts = await blogPostRepository.GetAllAsync();
            blogPosts = blogPosts.OrderByDescending(bp => bp.PublishedDate).ToList();

            // Calculate the total number of pages
            int totalPosts = blogPosts.Count();
            int totalPages = (int)Math.Ceiling((double)totalPosts / pageSize);


            var paginatedBlogPosts = blogPosts.Skip((page - 1) * pageSize).Take(pageSize).ToList();


            // get all tags
            var tags = await tagRepository.GetAllAsync();

            var model = new HomeViewModel
            {
                BlogPosts = paginatedBlogPosts,
                Tags = tags,


                // Set the TotalPages and Page properties
                TotalPages = totalPages,
                Page = page
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}