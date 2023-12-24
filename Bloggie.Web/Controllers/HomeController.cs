
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
            // Getting only visible blogs
            var blogPosts = (await blogPostRepository.GetAllAsync())
                                .Where(bp => bp.Visible)
                                .OrderByDescending(bp => bp.PublishedDate)
                                .ToList();

            // Calculate the total number of pages based on visible blog posts
            int totalPosts = blogPosts.Count();
            int totalPages = (int)Math.Ceiling((double)totalPosts / pageSize);

            // Validate the page parameter
            page = Math.Max(1, Math.Min(page, totalPages));

            // Get the paginated visible blog posts
            var paginatedBlogPosts = blogPosts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Get all tags
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