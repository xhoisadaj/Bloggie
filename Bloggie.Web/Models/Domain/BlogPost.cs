﻿namespace Bloggie.Web.Models.Domain
{
    public class BlogPost
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

        // New property for document file name
     

        // New property for UserId
        public string UserId { get; set; }

        // Navigation property
        public ICollection<Tag> Tags { get; set; }

        public ICollection<BlogPostLike> Likes { get; set; }
        public ICollection<BlogPostComment> Comments { get; set; }

        public ICollection<DocumentFileNames> DocumentFileNames { get; set; }
    }
}
