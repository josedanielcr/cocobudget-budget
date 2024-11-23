using Microsoft.EntityFrameworkCore;
using web_api.Database;
using web_api.Entities;

namespace web_api.Extensions;

public static class FolderExtensions
{
    public static async Task<Folder> GetFolderAsync(this ApplicationDbContext dbContext, Guid folderId, CancellationToken cancellationToken)
    {
        return await dbContext.Folders
            .FirstOrDefaultAsync(x => x.Id == folderId, cancellationToken) ?? throw new Exception("Folder not found");
    }
}