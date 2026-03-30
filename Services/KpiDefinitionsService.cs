using Microsoft.EntityFrameworkCore;
using KPIAPI.Data;
using KPIAPI.Domain.Constants;

namespace KPIAPI.Services
{
    public class KpiDefinitionsService
    {
        private readonly AppDbContext _db;

        public KpiDefinitionsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<object?> ListAsync(string robotKey, bool activeOnly, bool developerMode = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return null;

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return null;

            var query = _db.KpiDefinitions
                .AsNoTracking()
                .Where(d => d.RobotId == robot.Id);

            if (activeOnly)
                query = query.Where(d => d.IsActive);

            var defs = await query
                .OrderBy(d => d.Key)
                .Select(d => new
                {
                    d.Key,
                    d.Name,
                    d.Unit,
                    ValueType = d.ValueType,
                    d.IsActive,
                    d.CreatedUtc
                })
                .ToListAsync();

            return defs;
        }
    }
}