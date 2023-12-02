using Bloggie.Web.Models;
using Bloggie.Web.Models.ViewModels;
using Bloggie.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Bloggie.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogPostRepository blogPostRepository;
        private readonly ITagRepository tagRepository;

        public HomeController(ILogger<HomeController> logger,
            IBlogPostRepository blogPostRepository,
            ITagRepository tagRepository
            )
        {
            _logger = logger;
            this.blogPostRepository = blogPostRepository;
            this.tagRepository = tagRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
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