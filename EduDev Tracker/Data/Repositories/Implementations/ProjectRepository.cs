using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public class ProjectRepository: BaseRepository<Project>
    {
        public ProjectRepository(DatabaseService db) : base(db) { }

        public Task<List<Project>> GetByProfileAsync(int profileId) =>
            Connection.Table<Project>()
                .Where(p => p.ProfileId == profileId && !p.IsArchived)
                .ToListAsync();

        public async Task SaveAsync(Project project)
        {
            if (project.Id == 0)
            {
                project.CreatedAt = DateTime.UtcNow;
                await Connection.InsertAsync(project);
            }
            else
            {
                await Connection.UpdateAsync(project);
            }
        }
    }
}
