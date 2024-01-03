using Bloggie.Web.Models.Domain;
using Bloggie.Web.Models.ViewModels;
using Bloggie.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace Bloggie.Web.Controllers
{
    [Authorize]
    public class AdminBlogPostsController : Controller
    {
        private readonly ITagRepository tagRepository;
        private readonly IBlogPostRepository blogPostRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;

        public AdminBlogPostsController(ITagRepository tagRepository, IBlogPostRepository blogPostRepository, IWebHostEnvironment hostingEnvironment, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            this.tagRepository = tagRepository;
            this.blogPostRepository = blogPostRepository;
            _hostingEnvironment = hostingEnvironment;
            this.signInManager = signInManager;
            this.userManager = userManager;
        }


        [HttpGet]
        public async Task<IActionResult> Add()
        {
            // get tags from repository
            var tags = await tagRepository.GetAllAsync();

            var model = new AddBlogPostRequest
            {
                Tags = tags.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddBlogPostRequest addBlogPostRequest)
        {
            if (signInManager.IsSignedIn(User))
            {
                var userId = userManager.GetUserId(User);

                // Map view model to domain model
                var blogPost = new BlogPost
                {
                    Heading = addBlogPostRequest.Heading,
                    PageTitle = addBlogPostRequest.PageTitle,
                    Content = addBlogPostRequest.Content,
                    ShortDescription = addBlogPostRequest.ShortDescription,
                    FeaturedImageUrl = addBlogPostRequest.FeaturedImageUrl,
                    UrlHandle = addBlogPostRequest.UrlHandle,
                    PublishedDate = addBlogPostRequest.PublishedDate,
                    Author = addBlogPostRequest.Author,
                    Visible = addBlogPostRequest.Visible,
                    DocumentFileName = addBlogPostRequest.DocumentUpload != null ?
                                      await SaveDocumentLocally(addBlogPostRequest.DocumentUpload) :
                                      null,
                };

                // Map Tags from selected tags
                var selectedTags = new List<Tag>();
                foreach (var selectedTagId in addBlogPostRequest.SelectedTags)
                {
                    var selectedTagIdAsGuid = Guid.Parse(selectedTagId);
                    var existingTag = await tagRepository.GetAsync(selectedTagIdAsGuid);

                    if (existingTag != null)
                    {
                        selectedTags.Add(existingTag);
                    }
                }

                // Mapping tags back to domain model
                blogPost.Tags = selectedTags;

                var result = await blogPostRepository.AddAsync(blogPost, userId);

                // Check if the operation was successful
                if (result != null)
                {
                    // Set success message in TempData
                    TempData["SuccessMessage"] = "Blog post added successfully!";
                    return RedirectToAction("List"); // Redirect to the blog post list
                }
                else
                {
                    // Handle the case where adding the blog post failed
                    TempData["ErrorMessage"] = "Error adding the blog post. Please try again.";
                    return View(addBlogPostRequest); // Return to the add view with the model
                }
            }

            // If not signed in, return to the add view with the model
            return View(addBlogPostRequest);
        }

        private async Task<string> SaveDocumentLocally(IFormFile documentUpload)
        {
            if (documentUpload == null || documentUpload.Length == 0)
            {
                // No file uploaded
                return null;
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(documentUpload.FileName);
            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "documents", fileName);

            // Log the file path
            Console.WriteLine($"File will be saved to: {filePath}");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await documentUpload.CopyToAsync(fileStream);
            }

            return fileName;
        }


        [HttpGet]
        public async Task<IActionResult> List(int? page)
        {
            const int pageSize = 5; // Number of posts per page

            IEnumerable<BlogPost> blogPosts; // Change the type to IEnumerable<BlogPost>

            if (User.IsInRole("Admin"))
            {
                // Admin view: Retrieve all blog posts
                blogPosts = await blogPostRepository.GetAllAsync();
            }
            else
            {
                // User view: Retrieve the user's own blog posts
                var userId = userManager.GetUserId(User);
                blogPosts = await blogPostRepository.GetByUserIdAsync(userId);
            }

            // Sort blog posts by creation date in descending order
            blogPosts = blogPosts.OrderByDescending(post => post.PublishedDate);

            // Ensure page has a value, default to 1 if not
            var pageIndex = page.HasValue ? page.Value : 1;

            // Perform calculations using pageIndex
            var paginatedBlogPosts = blogPosts.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList(); // Explicitly convert to List<BlogPost>

            ViewBag.PageIndex = pageIndex;
            ViewBag.TotalPages = (int)Math.Ceiling(blogPosts.Count() / (double)pageSize);

            return View(paginatedBlogPosts);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            // Retrieve the result from the repository 
            var blogPost = await blogPostRepository.GetAsync(id);
            var tagsDomainModel = await tagRepository.GetAllAsync();

            if (blogPost != null)
            {
                // map the domain model into the view model
                var model = new EditBlogPostRequest
                {
                    Id = blogPost.Id,
                    Heading = blogPost.Heading,
                    PageTitle = blogPost.PageTitle,
                    Content = blogPost.Content,
                    Author = blogPost.Author,
                    FeaturedImageUrl = blogPost.FeaturedImageUrl,
                    UrlHandle = blogPost.UrlHandle,
                    ShortDescription = blogPost.ShortDescription,
                    PublishedDate = blogPost.PublishedDate,
                    DocumentFileName = blogPost.DocumentFileName,
                    Visible = blogPost.Visible,
                    Tags = tagsDomainModel.Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    }),
                    SelectedTags = blogPost.Tags.Select(x => x.Id.ToString()).ToArray()
                };

                return View(model);
            }

            // Pass data to view
            return View(null);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(EditBlogPostRequest editBlogPostRequest, IFormFile documentUpload)
        {
            // Retrieve the existing blog post from the repository
            var existingBlog = await blogPostRepository.GetAsync(editBlogPostRequest.Id);

            if (existingBlog == null)
            {
                // Handle the case where the blog post with the provided Id is not found
                // You might want to add some error handling or redirect to an error page
                return RedirectToAction("List");
            }

            // Map view model back to domain model
            var blogPostDomainModel = new BlogPost
            {
                Id = editBlogPostRequest.Id,
                Heading = editBlogPostRequest.Heading,
                PageTitle = editBlogPostRequest.PageTitle,
                Content = editBlogPostRequest.Content,
                Author = editBlogPostRequest.Author,
                ShortDescription = editBlogPostRequest.ShortDescription,
                FeaturedImageUrl = editBlogPostRequest.FeaturedImageUrl,
                PublishedDate = editBlogPostRequest.PublishedDate,
                UrlHandle = editBlogPostRequest.UrlHandle,
                Visible = editBlogPostRequest.Visible,
            };

            // Map tags into domain model
            var selectedTags = new List<Tag>();
            foreach (var selectedTag in editBlogPostRequest.SelectedTags)
            {
                if (Guid.TryParse(selectedTag, out var tag))
                {
                    var foundTag = await tagRepository.GetAsync(tag);

                    if (foundTag != null)
                    {
                        selectedTags.Add(foundTag);
                    }
                }
            }

            blogPostDomainModel.Tags = selectedTags;

            // Check if a new document is uploaded in the edit form
            if (documentUpload != null && documentUpload.Length > 0)
            {
                // Save the new document locally
                blogPostDomainModel.DocumentFileName = await SaveDocumentLocally(documentUpload);
            }
            else
            {
                // No new document uploaded, retain the existing DocumentFileName
                blogPostDomainModel.DocumentFileName = existingBlog.DocumentFileName;
            }

            // Log the document file name
            Console.WriteLine($"DocumentFileName: {blogPostDomainModel.DocumentFileName}");

            // Update other properties and DocumentFileName in the existing blog post
            existingBlog.Heading = blogPostDomainModel.Heading;
            existingBlog.PageTitle = blogPostDomainModel.PageTitle;
            existingBlog.Content = blogPostDomainModel.Content;
            existingBlog.Author = blogPostDomainModel.Author;
            existingBlog.ShortDescription = blogPostDomainModel.ShortDescription;
            existingBlog.FeaturedImageUrl = blogPostDomainModel.FeaturedImageUrl;
            existingBlog.PublishedDate = blogPostDomainModel.PublishedDate;
            existingBlog.UrlHandle = blogPostDomainModel.UrlHandle;
            existingBlog.Visible = blogPostDomainModel.Visible;
            existingBlog.Tags = blogPostDomainModel.Tags;
            existingBlog.DocumentFileName = blogPostDomainModel.DocumentFileName;

            // Update the existing blog post in the repository
            var updatedBlog = await blogPostRepository.UpdateAsync(existingBlog);

            if (updatedBlog != null)
            {
                // Show success notification
                return RedirectToAction("Edit", new { id = editBlogPostRequest.Id });
            }

            // Show error notification
            return RedirectToAction("Edit", new { id = editBlogPostRequest.Id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVisibility(Guid id, bool isVisible)
        {
            var existingBlog = await blogPostRepository.GetAsync(id);

            if (existingBlog == null)
            {
                // Handle the case where the blog post with the provided Id is not found
                // You might want to add some error handling or redirect to an error page
                return RedirectToAction("List");
            }

            // Update visibility status
            existingBlog.Visible = isVisible;

            // Save changes
            await blogPostRepository.UpdateAsync(existingBlog);

            // Redirect back to the list or wherever appropriate
            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> UserBlogPosts()
        {
            if (signInManager.IsSignedIn(User))
            {
                var userId = userManager.GetUserId(User);

                // Get blog posts associated with the logged-in user
                var userBlogPosts = await blogPostRepository.GetByUserIdAsync(userId);

                return View(userBlogPosts);
            }

            // If not signed in, you may want to redirect to the login page or handle it accordingly
            return RedirectToAction("Login", "Account");
        }


        [HttpPost]
        public async Task<IActionResult> Delete(EditBlogPostRequest editBlogPostRequest)
        {
            // Talk to repository to delete this blog post and tags
            var deletedBlogPost = await blogPostRepository.DeleteAsync(editBlogPostRequest.Id);

            if (deletedBlogPost != null)
            {
                // Show success notification
                return RedirectToAction("List");
            }

            // Show error notification
            return RedirectToAction("Edit", new { id = editBlogPostRequest.Id });
        }


    }
}
