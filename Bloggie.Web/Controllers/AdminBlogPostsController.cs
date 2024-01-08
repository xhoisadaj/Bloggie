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
        private readonly IDocumentFileNamesRepository documentFileNamesRepository;

        public AdminBlogPostsController(ITagRepository tagRepository, IBlogPostRepository blogPostRepository, IWebHostEnvironment hostingEnvironment, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IDocumentFileNamesRepository documentFileNamesRepository)
        {
            this.tagRepository = tagRepository;
            this.blogPostRepository = blogPostRepository;
            _hostingEnvironment = hostingEnvironment;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.documentFileNamesRepository = documentFileNamesRepository;
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
        public async Task<IActionResult> Add(AddBlogPostRequest addBlogPostRequest, List<IFormFile> documentUploads)
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
                    PublishedDate = addBlogPostRequest.PublishedDate.Date + DateTime.Now.TimeOfDay,
                    Author = addBlogPostRequest.Author,
                    Visible = addBlogPostRequest.Visible,
                    UserId = userId,
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

                // Save the BlogPost to the database first
                var result = await blogPostRepository.AddAsync(blogPost, userId);

                // Check if the operation was successful
                if (result != null)
                {
                    var documentFileNames = new List<DocumentFileNames>();

                    // Check if documents were uploaded9
                    // Save each document locally and create DocumentFileNames entities
                    foreach (var documentUpload in documentUploads)
                    {
                        var (documentFileName, documentFilePath) = await SaveDocumentLocally(documentUpload);

                        // Create DocumentFileNames entity
                        var documentEntity = new DocumentFileNames
                        {
                            FileName = documentFileName,
                            FilePath = documentFilePath,
                            BlogPostId = blogPost.Id,
                            Id = Guid.NewGuid(),
                        };

                        documentFileNames.Add(documentEntity);
                    }

                    // Save the DocumentFileNames entities to the database
                    await documentFileNamesRepository.AddRangeAsync(documentFileNames);

                    // Set DocumentFileNames property of the blog post
                    blogPost.DocumentFileNames = documentFileNames;

                    // Update the blog post to include the associated documents
                    await blogPostRepository.UpdateAsync(blogPost);

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

        private async Task<(string FileName, string FilePath)> SaveDocumentLocally(IFormFile documentUpload)
        {
            if (documentUpload == null || documentUpload.Length == 0)
            {
                // No file uploaded
                return (null, null);
            }

            // Get the original filename
            var originalFileName = Path.GetFileName(documentUpload.FileName);

            // Combine the original filename with a unique identifier (if needed)
           
            
            var fileName = $"{originalFileName}";

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "documents", fileName);

            // Log the file path
            Console.WriteLine($"File will be saved to: {filePath}");

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await documentUpload.CopyToAsync(fileStream);
            }

            return (fileName, filePath);
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
            var blogPost = await blogPostRepository.GetAsync(id);
            var tagsDomainModel = await tagRepository.GetAllAsync();

            if (blogPost != null)
            {
                // Get document information for the blog post
                var documentFileNames = await documentFileNamesRepository.GetDocumentsByBlogPostIdAsync(blogPost.Id);

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
                    Visible = blogPost.Visible,
                    Tags = tagsDomainModel.Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    }),
                    SelectedTags = blogPost.Tags.Select(x => x.Id.ToString()).ToArray(),
                    DocumentFileNames = documentFileNames.Select(df => df.FileName).ToList() // Add this line to populate DocumentFileNames in the view model
                };

                return View(model);
            }

            // Pass data to view
            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditBlogPostRequest editBlogPostRequest, List<IFormFile> documentUploads)
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
                PublishedDate = editBlogPostRequest.PublishedDate.Date + DateTime.Now.TimeOfDay,
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

            // Update other properties in the existing blog post
            existingBlog.Heading = blogPostDomainModel.Heading;
            existingBlog.PageTitle = blogPostDomainModel.PageTitle;
            existingBlog.Content = blogPostDomainModel.Content;
            existingBlog.Author = blogPostDomainModel.Author;
            existingBlog.ShortDescription = blogPostDomainModel.ShortDescription;
            existingBlog.FeaturedImageUrl = blogPostDomainModel.FeaturedImageUrl;
            existingBlog.PublishedDate = blogPostDomainModel.PublishedDate.Date + DateTime.Now.TimeOfDay;
            existingBlog.UrlHandle = blogPostDomainModel.UrlHandle;
            existingBlog.Visible = blogPostDomainModel.Visible;
            existingBlog.Tags = blogPostDomainModel.Tags;

            // Get existing document information for the blog post
            var existingDocuments = await documentFileNamesRepository.GetDocumentsByBlogPostIdAsync(existingBlog.Id);

            // Delete documents that were removed in the edit view
            var documentsToRemove = existingDocuments
                .Where(doc => !documentUploads.Any(uploadedDoc => uploadedDoc.FileName == doc.FileName))
                .ToList();

            foreach (var docToRemove in documentsToRemove)
            {
                // Remove from storage
                var docFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "documents", docToRemove.FileName);
                if (System.IO.File.Exists(docFilePath))
                {
                    System.IO.File.Delete(docFilePath);
                }

                // Remove from database
                await documentFileNamesRepository.DeleteAsync(docToRemove.Id);
            }

            // Save newly uploaded documents
            foreach (var documentUpload in documentUploads)
            {
                var (documentFileName, documentFilePath) = await SaveDocumentLocally(documentUpload);

                if (documentFileName != null)  // Ensure documentFileName is not null
                {
                    // Create DocumentFileNames entity
                    var documentEntity = new DocumentFileNames
                    {
                        FileName = documentFileName,
                        FilePath = documentFilePath,
                        BlogPostId = existingBlog.Id,
                        Id = Guid.NewGuid(),
                    };

                    await documentFileNamesRepository.AddAsync(documentEntity);
                }
            }


            // Save changes to the blog post
            var editingblogpost = await blogPostRepository.UpdateAsync(existingBlog);


            if (editingblogpost != null)
            {
                // Show success notification
            TempData["SuccessMessage"] = "Blog post edited successfully!";


            }
            else
            {
                // Show error notification
                TempData["ErrorMessage"] = "Error editing the blog post. Please try again.";

            }

            // Show success notification
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
            var updatedblogpost = await blogPostRepository.UpdateAsync(existingBlog);

            if (updatedblogpost != null)
            {
                // Show success notification
                TempData["SuccessMessage"] = "Blog post visibility updated successfully!";


            }
            else
            {
                // Show error notification
                TempData["ErrorMessage"] = "Error updating the visibility of the blog post. Please try again.";

            }
            TempData["SuccessMessage"] = "Blog post visibility updated successfully!";


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
        public async Task<IActionResult> Delete(Guid id)
        {
            // Get associated documents
            var associatedDocuments = await documentFileNamesRepository.GetDocumentsByBlogPostIdAsync(id);

            // Delete documents from storage
            foreach (var document in associatedDocuments)
            {
                var docFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "documents", document.FileName);
                if (System.IO.File.Exists(docFilePath))
                {
                    System.IO.File.Delete(docFilePath);
                }
            }

            // Delete blog post and associated documents from the database
            var deletedBlogPost = await blogPostRepository.DeleteAsync(id);

            // Show success or error notification based on the result
            if (deletedBlogPost != null)
            {
                // Delete associated documents from the database
                foreach (var document in associatedDocuments)
                {
                    await documentFileNamesRepository.DeleteAsync(document.Id);
                }
                TempData["SuccessMessage"] = "Blog post deleted successfully!";
                return RedirectToAction("List");
            }
       
            TempData["ErrorMessage"] = "Error deleting the blog post. Please try again.";
            // Show error notification
            return RedirectToAction("List");
        }


    }
}
