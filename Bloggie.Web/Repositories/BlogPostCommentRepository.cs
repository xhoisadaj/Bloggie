using Bloggie.Web.Data;
using Bloggie.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Bloggie.Web.Repositories
{
    public class BlogPostCommentRepository : IBlogPostCommentRepository
    {
        private readonly BloggieDbContext bloggieDbContext;

        public BlogPostCommentRepository(BloggieDbContext bloggieDbContext)
        {
            this.bloggieDbContext = bloggieDbContext;
        }

        public async Task<BlogPostComment> AddAsync(BlogPostComment blogPostComment)
        {
            await bloggieDbContext.BlogPostComment.AddAsync(blogPostComment);
            await bloggieDbContext.SaveChangesAsync();
            return blogPostComment;
        }

        public async Task<IEnumerable<BlogPostComment>> GetCommentsByBlogIdAsync(Guid blogPostId)
        {
            return await bloggieDbContext.BlogPostComment.Where(x => x.BlogPostId == blogPostId)
                .ToListAsync();
        }

        [Authorize(Roles = "Admin")]
        public async Task<bool> DeleteAsync(Guid commentId)
        {
            var commentToDelete = await GetCommentByIdAsync(commentId);

            if (commentToDelete != null)
            {
                bloggieDbContext.BlogPostComment.Remove(commentToDelete);
                await bloggieDbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        [Authorize(Roles = "Admin")]
        public async Task<BlogPostComment> GetCommentByIdAsync(Guid commentId)
        {
            return await bloggieDbContext.BlogPostComment.FindAsync(commentId);
        }
    }
}
