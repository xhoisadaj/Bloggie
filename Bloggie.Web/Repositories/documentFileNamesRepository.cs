using Bloggie.Web.Data;
using Bloggie.Web.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bloggie.Web.Repositories
{
    public class DocumentFileNamesRepository : IDocumentFileNamesRepository
    {
        private readonly BloggieDbContext _dbContext;

        public DocumentFileNamesRepository(BloggieDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DocumentFileNames> AddAsync(DocumentFileNames documentFileNames)
        {
            if (documentFileNames == null)
            {
                throw new ArgumentNullException(nameof(documentFileNames));
            }

            await _dbContext.DocumentFileNames.AddAsync(documentFileNames);
            await _dbContext.SaveChangesAsync();

            return documentFileNames;
        }

        public async Task AddRangeAsync(IEnumerable<DocumentFileNames> documentFileNames)
        {
            if (documentFileNames == null)
            {
                throw new ArgumentNullException(nameof(documentFileNames));
            }

            await _dbContext.DocumentFileNames.AddRangeAsync(documentFileNames);
            await _dbContext.SaveChangesAsync();
        }

        // Add other repository methods as needed

        public async Task<List<DocumentFileNames>> GetAllAsync()
        {
            return await _dbContext.DocumentFileNames.ToListAsync();
        }

        public async Task<List<DocumentFileNames>> GetDocumentsByBlogPostIdAsync(Guid blogPostId)
        {
            return await _dbContext.DocumentFileNames
                .Where(df => df.BlogPostId == blogPostId)
                .ToListAsync();
        }

        public async Task DeleteAsync(Guid documentId)
        {
            var document = await _dbContext.DocumentFileNames.FindAsync(documentId);
            if (document != null)
            {
                _dbContext.DocumentFileNames.Remove(document);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

  
}
