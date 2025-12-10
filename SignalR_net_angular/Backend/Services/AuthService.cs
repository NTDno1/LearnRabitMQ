using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký user mới
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra username đã tồn tại chưa
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return null; // Username đã tồn tại
        }

        // Kiểm tra email đã tồn tại chưa
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return null; // Email đã tồn tại
        }

        // Hash password
        var passwordHash = HashPassword(request.Password);

        // Tạo user mới
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName ?? request.Username,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User registered: {user.Username}");

        // Tạo token đơn giản (có thể thay bằng JWT sau)
        var token = GenerateSimpleToken(user.Id, user.Username);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = token
        };
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            return null; // User không tồn tại
        }

        // Kiểm tra password
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return null; // Password sai
        }

        // Cập nhật trạng thái online
        user.IsOnline = true;
        user.LastSeen = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation($"User logged in: {user.Username}");

        // Tạo token
        var token = GenerateSimpleToken(user.Id, user.Username);

        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Token = token
        };
    }

    /// <summary>
    /// Xác thực token và lấy user
    /// </summary>
    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            // Parse token đơn giản: userId:username:timestamp
            var parts = token.Split(':');
            if (parts.Length < 2)
                return null;

            var userId = int.Parse(parts[0]);
            var username = parts[1];

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Username != username)
                return null;

            return user;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Hash password bằng SHA256
    /// </summary>
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Verify password
    /// </summary>
    private bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }

    /// <summary>
    /// Tạo token đơn giản (có thể thay bằng JWT sau)
    /// Format: userId:username:timestamp
    /// </summary>
    private string GenerateSimpleToken(int userId, string username)
    {
        var timestamp = DateTime.UtcNow.Ticks;
        return $"{userId}:{username}:{timestamp}";
    }
}

