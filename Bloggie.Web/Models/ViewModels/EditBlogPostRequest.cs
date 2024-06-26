﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bloggie.Web.Models.ViewModels
{
    public class EditBlogPostRequest
    {
        public Guid Id { get; set; }
        public string Heading { get; set; }
        public string PageTitle { get; set; }
        public string Content { get; set; }
        public string ShortDescription { get; set; }
        public string FeaturedImageUrl { get; set; }
        public string UrlHandle { get; set; }
        public DateTime PublishedDate { get; set; }
        public string Author { get; set; }
        public bool Visible { get; set; }

        [Display(Name = "Document File Names")]
        public ICollection<string> DocumentFileNames { get; set; } // Add this line

        [Display(Name = "Document Upload")]
        public List<IFormFile> DocumentUploads { get; set; } // Modify this line


        [Display(Name = "Document Upload")]
        public IFormFile DocumentUpload { get; set; }
        // Display tags
        public IEnumerable<SelectListItem> Tags { get; set; }
        // Collect Tag
        public string[] SelectedTags { get; set; } = Array.Empty<string>();
    }
}
