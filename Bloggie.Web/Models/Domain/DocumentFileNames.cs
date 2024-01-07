namespace Bloggie.Web.Models.Domain
{
    public class DocumentFileNames
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }

        // Additional properties related to documents, if needed

        // Foreign key to associate the document with a blog post
        public Guid BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }



    }
}
