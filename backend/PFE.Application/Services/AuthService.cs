using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PFE.Application.DTOs.Auth;
using PFE.Application.DTOs.User;
using PFE.Domain.Entities;
using PFE.Application.Abstractions;
using BCrypt.Net;

namespace PFE.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(IApplicationDbContext context, IJwtService jwtService, IMapper mapper)
    {
        _context = context;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new Exception("PasswordHash is empty for this user.");
        }

        bool isPasswordValid;

        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        }
        catch
        {
            throw new Exception("Invalid PasswordHash in database. This user password is not correctly hashed.");
        }

        if (!isPasswordValid)
        {
            return null;
        }

        if (!user.IsActive)
        {
            return new AuthResponseDto
            {
                Token = string.Empty,
                User = null
            };
        }

        if (user.Role == null)
        {
            throw new Exception("User role is missing.");
        }

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role.Name);
        var userDto = _mapper.Map<UserDto>(user);

        return new AuthResponseDto
        {
            Token = token,
            User = userDto
        };
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            return null;
        }

        // Ignore client-provided Role and LeaveBalance - always set defaults
        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            FullName = registerDto.FullName,
            RoleId = 1, // Always Employee, ignore client input
            DepartmentId = registerDto.DepartmentId,
            PhoneNumber = registerDto.PhoneNumber,
            LeaveBalance = null, // Null until admin approval, ignore client input
            IsActive = false, // Inactive until admin approval
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Return empty token - user cannot login until approved
        return new AuthResponseDto
        {
            Token = string.Empty,
            User = null
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return _mapper.Map<UserDto>(user);
    }
}
