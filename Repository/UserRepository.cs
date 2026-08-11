using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Quản_lý_quán_cafe.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id && !u.IsDeleted);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            username = username.Trim();
            var normalizedLogin = username.ToLowerInvariant();
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => !u.IsDeleted &&
                    (u.Username.ToLower() == normalizedLogin
                     || (u.Employee != null && u.Employee.Email.ToLower() == normalizedLogin)
                     || (u.Customer != null && u.Customer.Email != null && u.Customer.Email.ToLower() == normalizedLogin)));
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => !u.IsDeleted &&
                    ((u.Employee != null && !u.Employee.IsDeleted && u.Employee.Email.ToLower() == normalizedEmail)
                     || (u.Customer != null && !u.Customer.IsDeleted && u.Customer.Email != null
                         && u.Customer.Email.ToLower() == normalizedEmail)));
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleID == roleId && !r.IsDeleted);
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await GetByUsernameAsync(username);
            if (user == null)
            {
                return null;
            }

            var isStaff = user.Role?.RoleName is "Admin" or "Cashier";
            if (!user.IsActive
                || (isStaff && user.Employee is null)
                || user.Employee?.IsActive == false || user.Employee?.IsDeleted == true
                || user.Customer?.IsActive == false || user.Customer?.IsDeleted == true)
                return null;


            if (!VerifyPasswordHash(password, user.PasswordHash))
            {
                return null;
            }

            if (!user.PasswordHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
                user.PasswordHash = HashPassword(password);
            user.LastLogin = DateTime.UtcNow;

            return user;
        }

        public async Task AddAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var entry = _context.Entry(user);
            if (entry.State == EntityState.Detached)
                _context.Users.Attach(user);
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        public static string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            const int iterations = 210_000;
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPasswordHash(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;
            if (hash.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                var parts = hash.Split('$');
                if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 100_000)
                    return false;
                try
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    var expected = Convert.FromBase64String(parts[3]);
                    var actual = Rfc2898DeriveBytes.Pbkdf2(
                        password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // Compatibility with databases created by earlier versions. A
            // successful login is immediately upgraded to PBKDF2 above.
            var legacy = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            byte[] expectedLegacy;
            try { expectedLegacy = Convert.FromBase64String(hash); }
            catch (FormatException) { return false; }
            return CryptographicOperations.FixedTimeEquals(legacy, expectedLegacy);
        }
    }
}
