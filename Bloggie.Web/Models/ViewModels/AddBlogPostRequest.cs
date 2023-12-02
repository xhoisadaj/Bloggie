using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bloggie.Web.Models.ViewModels
{
    public class AddBlogPostRequest
    {
        [Required(ErrorMessage = "Heading is required.")]
        public string Heading { get; set; }

        [Required(ErrorMessage = "Page Title is required.")]
        public string PageTitle { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Short Description is required.")]
        public string ShortDescription { get; set; }
        [Required(ErrorMessage = "Featured image url is required.")]

        public string FeaturedImageUrl { get; set; }
        [Required(ErrorMessage = "Url handle is required.")]

        public string UrlHandle { get; set; }

        [Required(ErrorMessage = "Published Date is required.")]
        public DateTime PublishedDate { get; set; }

        [Required(ErrorMessage = "Author is required.")]
        public string Author { get; set; }
        public bool Visible { get; set; }


        // Display tags
        public IEnumerable<SelectListItem> Tags { get; set; }
        // Collect Tag
        [Required(ErrorMessage = "At least one tag must be selected.")]
        public string[] SelectedTags { get; set; } = Array.Empty<string>();
    }
}
