using Bloggie.Web.Models.Domain;

namespace Bloggie.Web.Repositories
{

    public interface IDocumentFileNamesRepository
    {
        Task<DocumentFileNames> AddAsync(DocumentFileNames documentFileNames);
        Task AddRangeAsync(IEnumerable<DocumentFileNames> documentFileNames);
        Task<List<DocumentFileNames>> GetAllAsync();
        Task<List<DocumentFileNames>> GetDocumentsByBlogPostIdAsync(Guid blogPostId);
        Task DeleteAsync(Guid documentId);
    }
}
