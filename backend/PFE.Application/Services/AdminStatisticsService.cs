using Microsoft.EntityFrameworkCore;
using PFE.Application.Abstractions;
using PFE.Application.DTOs.Admin;

namespace PFE.Application.Services;

public class AdminStatisticsService : IAdminStatisticsService
{
    private readonly IApplicationDbContext _db;

    public AdminStatisticsService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminStatisticsDto> GetStatisticsAsync(DateTime? from, DateTime? to, int? departmentId)
    {
        var (fromStart, toEnd) = NormalizeRange(from, to);
        var deptId = departmentId is > 0 ? departmentId : null;

        var usersQuery = _db.Users.AsNoTracking();
        if (deptId.HasValue)
            usersQuery = usersQuery.Where(u => u.DepartmentId == deptId.Value);

        var usersTotal = await usersQuery.CountAsync();
        var usersActive = await usersQuery.CountAsync(u => u.IsActive);
        var usersPending = await usersQuery.CountAsync(u => !u.IsActive);
        var usersRegisteredInPeriod = await usersQuery.CountAsync(u =>
            u.CreatedAt >= fromStart && u.CreatedAt <= toEnd);

        var infrastructure = new InfrastructureStatsDto
        {
            Departments = await _db.Departments.AsNoTracking().CountAsync(),
            Rooms = await _db.Rooms.AsNoTracking().CountAsync(),
            OfficeTables = await _db.OfficeTables.AsNoTracking().CountAsync(),
            Seats = await _db.Seats.AsNoTracking().CountAsync(),
        };

        var leaveBase = _db.LeaveRequests.AsNoTracking()
            .Where(l => l.StartDate <= toEnd && l.EndDate >= fromStart);
        if (deptId.HasValue)
            leaveBase = leaveBase.Where(l => l.User.DepartmentId == deptId.Value);

        var leaveOverlapping = await leaveBase.CountAsync();
        var leaveByStatus = await leaveBase
            .GroupBy(l => l.Status)
            .Select(g => new StatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var roomResBase = _db.RoomReservations.AsNoTracking()
            .Where(r => r.StartDateTime <= toEnd && r.EndDateTime >= fromStart);
        if (deptId.HasValue)
            roomResBase = roomResBase.Where(r => r.User.DepartmentId == deptId.Value);

        var roomResOverlapping = await roomResBase.CountAsync();
        var roomResByStatus = await roomResBase
            .GroupBy(r => r.Status)
            .Select(g => new StatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var seatResBase = _db.SeatReservations.AsNoTracking()
            .Where(s => s.Date >= fromStart && s.Date <= toEnd);
        if (deptId.HasValue)
            seatResBase = seatResBase.Where(s => s.User.DepartmentId == deptId.Value);

        var seatInPeriod = await seatResBase.CountAsync();
        var seatByStatus = await seatResBase
            .GroupBy(s => s.Status)
            .Select(g => new StatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var genBase = _db.GeneralRequests.AsNoTracking()
            .Where(g => g.CreatedAt >= fromStart && g.CreatedAt <= toEnd);
        if (deptId.HasValue)
            genBase = genBase.Where(g => g.User.DepartmentId == deptId.Value);

        var genCreated = await genBase.CountAsync();
        var genByStatus = await genBase
            .GroupBy(g => g.Status)
            .Select(g => new StatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var absBase = _db.AbsenceRequests.AsNoTracking()
            .Where(a => a.Date >= fromStart && a.Date <= toEnd);
        if (deptId.HasValue)
            absBase = absBase.Where(a => a.User.DepartmentId == deptId.Value);

        var absInPeriod = await absBase.CountAsync();
        var absByStatus = await absBase
            .GroupBy(a => a.Status)
            .Select(g => new StatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var eventsBase = _db.Events.AsNoTracking()
            .Where(e => e.StartDateTime >= fromStart && e.StartDateTime <= toEnd);
        if (deptId.HasValue)
            eventsBase = eventsBase.Where(e => e.CreatedByUser.DepartmentId == deptId.Value);

        var eventsCount = await eventsBase.CountAsync();
        var eventIds = await eventsBase.Select(e => e.Id).ToListAsync();
        var participantsCount = eventIds.Count == 0
            ? 0
            : await _db.EventParticipants.AsNoTracking().CountAsync(p => eventIds.Contains(p.EventId));

        var announcementsBase = _db.Announcements.AsNoTracking()
            .Where(a => a.CreatedAt >= fromStart && a.CreatedAt <= toEnd);
        if (deptId.HasValue)
            announcementsBase = announcementsBase.Where(a => a.CreatedBy.DepartmentId == deptId.Value);

        var announcementsCount = await announcementsBase.CountAsync();

        var internalBase = _db.InternalRequests.AsNoTracking()
            .Where(i => i.CreatedAt >= fromStart && i.CreatedAt <= toEnd);
        if (deptId.HasValue)
            internalBase = internalBase.Where(i => i.User.DepartmentId == deptId.Value);

        var internalCreated = await internalBase.CountAsync();
        var internalByStatus = await internalBase
            .GroupBy(i => i.Status)
            .Select(g => new StatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new AdminStatisticsDto
        {
            From = fromStart,
            To = toEnd,
            DepartmentId = deptId,
            Users = new UserStatsDto
            {
                Total = usersTotal,
                Active = usersActive,
                PendingApproval = usersPending,
                RegisteredInPeriod = usersRegisteredInPeriod,
            },
            Infrastructure = infrastructure,
            LeaveByStatus = leaveByStatus.OrderBy(x => x.Status).ToList(),
            LeaveRequestsOverlappingPeriod = leaveOverlapping,
            RoomReservationByStatus = roomResByStatus.OrderBy(x => x.Status).ToList(),
            RoomReservationsOverlappingPeriod = roomResOverlapping,
            SeatReservationByStatus = seatByStatus.OrderBy(x => x.Status).ToList(),
            SeatReservationsInPeriod = seatInPeriod,
            GeneralRequestByStatus = genByStatus.OrderBy(x => x.Status).ToList(),
            GeneralRequestsCreatedInPeriod = genCreated,
            AbsenceByStatus = absByStatus.OrderBy(x => x.Status).ToList(),
            AbsenceRequestsInPeriod = absInPeriod,
            EventsStartingInPeriod = eventsCount,
            EventParticipantsForEventsInPeriod = participantsCount,
            AnnouncementsCreatedInPeriod = announcementsCount,
            InternalRequestByStatus = internalByStatus.OrderBy(x => x.Status).ToList(),
            InternalRequestsCreatedInPeriod = internalCreated,
        };
    }

    private static (DateTime fromStart, DateTime toEnd) NormalizeRange(DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        var toDate = (to ?? today).Date;
        var fromDate = (from ?? today.AddDays(-29)).Date;

        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        var fromStart = fromDate;
        var toEnd = toDate.AddDays(1).AddTicks(-1);

        return (fromStart, toEnd);
    }
}
