using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PFE.Chatbot;
using PFE.Domain.Entities;
using PFE.Domain.Enums;
using PFE.Infrastructure.Data;
using Xunit;

namespace PFE.Chatbot.Tests;

public class DatabaseToolsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DatabaseTools _dbTools;

    public DatabaseToolsTests()
    {
        // Set up a unique in-memory database database name per test class instance (or test run)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _dbTools = new DatabaseTools(_context);
        SeedDatabase();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedDatabase()
    {
        // 1. Seed Roles
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Employee", Description = "Regular employee" },
            new() { Id = 2, Name = "Manager", Description = "Department manager" },
            new() { Id = 3, Name = "Admin", Description = "System administrator" }
        };
        _context.Roles.AddRange(roles);

        // 2. Seed Departments
        var depts = new List<Department>
        {
            new() { Id = 1, Name = "IT" },
            new() { Id = 2, Name = "HR" },
            new() { Id = 3, Name = "Finance" }
        };
        _context.Departments.AddRange(depts);

        // 3. Seed Rooms
        var rooms = new List<Room>
        {
            new() { Id = 1, Name = "Conference Room A", Type = RoomType.Conference, Capacity = 20, IsActive = true },
            new() { Id = 2, Name = "Meeting Room B", Type = RoomType.Meeting, Capacity = 10, IsActive = true },
            new() { Id = 3, Name = "Training Room", Type = RoomType.Training, Capacity = 30, IsActive = false } // Inactive
        };
        _context.Rooms.AddRange(rooms);
        _context.SaveChanges();

        // 4. Seed Users
        var users = new List<User>
        {
            new() { Id = 1, FullName = "Mouayad Admin", Email = "admin@checkpoint.com", PasswordHash = "hash", RoleId = 3, DepartmentId = 1, LeaveBalance = 30, IsActive = true },
            new() { Id = 2, FullName = "Sarah Bencherif", Email = "sarah.bencherif@checkpoint.com", PasswordHash = "hash", RoleId = 2, DepartmentId = 2, LeaveBalance = 25, IsActive = true },
            new() { Id = 3, FullName = "Ahmed Benali", Email = "ahmed.benali@checkpoint.com", PasswordHash = "hash", RoleId = 2, DepartmentId = 1, LeaveBalance = 25, IsActive = true },
            new() { Id = 4, FullName = "Fatima Zahra", Email = "fatima.zahra@checkpoint.com", PasswordHash = "hash", RoleId = 1, DepartmentId = 1, LeaveBalance = 20, IsActive = true },
            new() { Id = 5, FullName = "Youssef Amrani", Email = "youssef.amrani@checkpoint.com", PasswordHash = "hash", RoleId = 1, DepartmentId = 2, LeaveBalance = 18, IsActive = true },
            new() { Id = 6, FullName = "Nadia Inactive", Email = "nadia@checkpoint.com", PasswordHash = "hash", RoleId = 1, DepartmentId = 3, LeaveBalance = 22, IsActive = false } // Inactive employee
        };
        _context.Users.AddRange(users);

        // 5. Seed Events
        var events = new List<Event>
        {
            new() { Id = 1, Title = "Q3 Planning Meeting", Description = "Planning session", Type = EventType.Meeting, RoomId = 1, StartDateTime = DateTime.UtcNow.AddDays(1), EndDateTime = DateTime.UtcNow.AddDays(1).AddHours(2), CreatedByUserId = 1, IsMandatory = true, RSVPEnabled = true },
            new() { Id = 2, Title = "Cybersecurity Workshop", Description = "Security training", Type = EventType.Workshop, RoomId = 2, StartDateTime = DateTime.UtcNow.AddDays(2), EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(2), CreatedByUserId = 3, IsMandatory = true, RSVPEnabled = false }
        };
        _context.Events.AddRange(events);

        // 6. Seed LeaveRequests
        var leaves = new List<LeaveRequest>
        {
            new() { Id = 1, UserId = 4, StartDate = DateTime.UtcNow.AddDays(2).Date, EndDate = DateTime.UtcNow.AddDays(4).Date, Type = LeaveType.Vacation, Status = RequestStatus.Approved, Reason = "Vacation", AssignedManagerId = 3 },
            new() { Id = 2, UserId = 5, StartDate = DateTime.UtcNow.AddDays(7).Date, EndDate = DateTime.UtcNow.AddDays(7).Date, Type = LeaveType.Sick, Status = RequestStatus.Pending, Reason = "Flu", AssignedManagerId = 2 }
        };
        _context.LeaveRequests.AddRange(leaves);

        // 7. Seed Announcements
        var announcements = new List<Announcement>
        {
            new() { Id = 1, Title = "Active Announcement", Content = "Some active content", CreatedById = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Inactive Announcement", Content = "Some inactive content", CreatedById = 2, IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _context.Announcements.AddRange(announcements);

        // 8. Seed GeneralRequests
        var genRequests = new List<GeneralRequest>
        {
            new() { Id = 1, UserId = 4, Title = "Laptop broken", Description = "Screen is flickering", Category = RequestCategory.IT, Status = RequestStatus.InProgress, AssignedToUserId = 3, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 5, Title = "Chair missing", Description = "Need new office chair", Category = RequestCategory.Facilities, Status = RequestStatus.Pending, AssignedToUserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _context.GeneralRequests.AddRange(genRequests);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetDepartmentsAsync_ReturnsAllDepartmentsWithEmployeeCountsSorted()
    {
        // Act
        var jsonResult = await _dbTools.GetDepartmentsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;
        
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(3, root.GetArrayLength()); // IT, HR, Finance

        // Sort check: Finance, HR, IT
        Assert.Equal("Finance", root[0].GetProperty("Name").GetString());
        Assert.Equal("HR", root[1].GetProperty("Name").GetString());
        Assert.Equal("IT", root[2].GetProperty("Name").GetString());

        // Employee count check:
        // IT (Id=1) has 3 active users: Mouayad Admin (1), Ahmed Benali (3), Fatima Zahra (4)
        // HR (Id=2) has 2 active users: Sarah Bencherif (2), Youssef Amrani (5)
        // Finance (Id=3) has 0 active users: Nadia Inactive (6) is inactive
        Assert.Equal(0, root[0].GetProperty("EmployeeCount").GetInt32());
        Assert.Equal(2, root[1].GetProperty("EmployeeCount").GetInt32());
        Assert.Equal(3, root[2].GetProperty("EmployeeCount").GetInt32());
    }

    [Fact]
    public async Task GetEmployeesAsync_NoFilter_ReturnsAllActiveEmployees()
    {
        // Act
        var jsonResult = await _dbTools.GetEmployeesAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(5, root.GetArrayLength()); // Total 6 users minus 1 inactive user (Nadia)
        
        // Sorting check by FullName: Ahmed Benali, Fatima Zahra, Mouayad Admin, Sarah Bencherif, Youssef Amrani
        Assert.Equal("Ahmed Benali", root[0].GetProperty("FullName").GetString());
        Assert.Equal("Fatima Zahra", root[1].GetProperty("FullName").GetString());
        Assert.Equal("Mouayad Admin", root[2].GetProperty("FullName").GetString());
        Assert.Equal("Sarah Bencherif", root[3].GetProperty("FullName").GetString());
        Assert.Equal("Youssef Amrani", root[4].GetProperty("FullName").GetString());
    }

    [Fact]
    public async Task GetEmployeesAsync_WithDepartmentFilter_FiltersCorrectly()
    {
        // Act
        var jsonResult = await _dbTools.GetEmployeesAsync("HR");

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength()); // Sarah Bencherif, Youssef Amrani
        Assert.Equal("HR", root[0].GetProperty("Department").GetString());
        Assert.Equal("HR", root[1].GetProperty("Department").GetString());
    }

    [Fact]
    public async Task GetEmployeeDetailsAsync_ReturnsCorrectEmployeeDetailsForPartialMatch()
    {
        // Act
        var jsonResult = await _dbTools.GetEmployeeDetailsAsync("Zahra");

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());
        
        var employee = root[0];
        Assert.Equal("Fatima Zahra", employee.GetProperty("FullName").GetString());
        Assert.Equal("fatima.zahra@checkpoint.com", employee.GetProperty("Email").GetString());
        Assert.Equal("IT", employee.GetProperty("Department").GetString());
        Assert.Equal("Employee", employee.GetProperty("Role").GetString());
        Assert.Equal(20, employee.GetProperty("LeaveBalance").GetInt32());
    }

    [Fact]
    public async Task GetLeaveRequestsAsync_NoFilter_ReturnsAllLeaveRequests()
    {
        // Act
        var jsonResult = await _dbTools.GetLeaveRequestsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
        
        // Sorting should be descending by Id (newest first, assuming Id auto-increments or is ordered)
        Assert.Equal(2, root[0].GetProperty("Id").GetInt32());
        Assert.Equal("Youssef Amrani", root[0].GetProperty("Employee").GetString());
        
        Assert.Equal(1, root[1].GetProperty("Id").GetInt32());
        Assert.Equal("Fatima Zahra", root[1].GetProperty("Employee").GetString());
    }

    [Fact]
    public async Task GetLeaveRequestsAsync_WithStatusFilter_FiltersCorrectly()
    {
        // Act
        var jsonResult = await _dbTools.GetLeaveRequestsAsync("Approved");

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal("Approved", root[0].GetProperty("Status").GetString());
        Assert.Equal("Fatima Zahra", root[0].GetProperty("Employee").GetString());
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsUpcomingEventsWithDetailsOrderedByDate()
    {
        // Act
        var jsonResult = await _dbTools.GetEventsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());

        // First event (Q3 Planning Meeting) is tomorrow, second (Cybersecurity Workshop) is in 2 days.
        Assert.Equal("Q3 Planning Meeting", root[0].GetProperty("Title").GetString());
        Assert.Equal("Conference Room A", root[0].GetProperty("RoomName").GetString());
        Assert.Equal("Mouayad Admin", root[0].GetProperty("CreatedBy").GetString());

        Assert.Equal("Cybersecurity Workshop", root[1].GetProperty("Title").GetString());
        Assert.Equal("Meeting Room B", root[1].GetProperty("RoomName").GetString());
        Assert.Equal("Ahmed Benali", root[1].GetProperty("CreatedBy").GetString());
    }

    [Fact]
    public async Task GetRoomsAsync_ReturnsAllRoomsOrderedByName()
    {
        // Act
        var jsonResult = await _dbTools.GetRoomsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(3, root.GetArrayLength());

        // Alphabetically: Conference Room A, Meeting Room B, Training Room
        Assert.Equal("Conference Room A", root[0].GetProperty("Name").GetString());
        Assert.Equal("Meeting Room B", root[1].GetProperty("Name").GetString());
        Assert.Equal("Training Room", root[2].GetProperty("Name").GetString());
        
        Assert.True(root[0].GetProperty("IsActive").GetBoolean());
        Assert.True(root[1].GetProperty("IsActive").GetBoolean());
        Assert.False(root[2].GetProperty("IsActive").GetBoolean()); // Inactive room
    }

    [Fact]
    public async Task GetAnnouncementsAsync_ReturnsOnlyActiveAnnouncementsOrderedByDateDescending()
    {
        // Act
        var jsonResult = await _dbTools.GetAnnouncementsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength()); // 1 active, 1 inactive
        Assert.Equal("Active Announcement", root[0].GetProperty("Title").GetString());
        Assert.True(root[0].GetProperty("IsActive").GetBoolean());
        Assert.Equal("Sarah Bencherif", root[0].GetProperty("CreatedBy").GetString());
    }

    [Fact]
    public async Task GetGeneralRequestsAsync_ReturnsAllRequestsOrderedByDateDescending()
    {
        // Act
        var jsonResult = await _dbTools.GetGeneralRequestsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
        
        // Laptop broken (created more recently) should be first
        Assert.Equal("Laptop broken", root[0].GetProperty("Title").GetString());
        Assert.Equal("InProgress", root[0].GetProperty("Status").GetString());
        Assert.Equal("Ahmed Benali", root[0].GetProperty("AssignedTo").GetString());

        Assert.Equal("Chair missing", root[1].GetProperty("Title").GetString());
        Assert.Equal("Pending", root[1].GetProperty("Status").GetString());
        Assert.Equal("Mouayad Admin", root[1].GetProperty("AssignedTo").GetString());
    }

    [Fact]
    public async Task GetGeneralRequestsAsync_WithStatusFilter_FiltersCorrectly()
    {
        // Act
        var jsonResult = await _dbTools.GetGeneralRequestsAsync("Pending");

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal("Chair missing", root[0].GetProperty("Title").GetString());
        Assert.Equal("Pending", root[0].GetProperty("Status").GetString());
    }

    [Fact]
    public async Task GetStatisticsAsync_AggregatesCorrectCounts()
    {
        // Act
        var jsonResult = await _dbTools.GetStatisticsAsync();

        // Assert
        Assert.NotNull(jsonResult);
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;

        // Statistics count check:
        // - active employees: 5 (Admin, Sarah, Ahmed, Fatima, Youssef)
        // - departments: 3 (IT, HR, Finance)
        // - active rooms: 2 (Conference A, Meeting B - Training is inactive)
        // - events: 2
        // - pending leave requests: 1 (Youssef is Pending, Fatima is Approved)
        // - pending general requests: 1 (Chair missing is Pending, Laptop broken is InProgress)
        // - active announcements: 1
        Assert.Equal(5, root.GetProperty("total_active_employees").GetInt32());
        Assert.Equal(3, root.GetProperty("total_departments").GetInt32());
        Assert.Equal(2, root.GetProperty("total_active_rooms").GetInt32());
        Assert.Equal(2, root.GetProperty("total_events").GetInt32());
        Assert.Equal(1, root.GetProperty("pending_leave_requests").GetInt32());
        Assert.Equal(1, root.GetProperty("pending_general_requests").GetInt32());
        Assert.Equal(1, root.GetProperty("active_announcements").GetInt32());
    }
}
