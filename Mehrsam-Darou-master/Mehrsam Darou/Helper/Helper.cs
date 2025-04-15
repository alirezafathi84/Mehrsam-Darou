using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Mehrsam_Darou.Helper
{
    public static class Helper
    {
        // Hash the password using PBKDF2 and return the salt and hash as a combined string
        public static string HashPassword(string password)
        {
            byte[] salt = GenerateSalt();
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8);

            // Combine salt and hash into a single string for storage
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        // Verify a password against a stored hash
        public static bool VerifyPassword(string storedHash, string passwordToCheck)
        {
            string[] parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                throw new FormatException("Invalid stored hash format.");
            }

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHashBytes = Convert.FromBase64String(parts[1]);

            byte[] hashToCheck = KeyDerivation.Pbkdf2(
                password: passwordToCheck,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8);

            return CryptographicOperations.FixedTimeEquals(storedHashBytes, hashToCheck);
        }

        // Generate a cryptographically strong random salt
        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }




        public class PaginatedList<T> : List<T>
        {
            public int PageIndex { get; private set; }
            public int TotalPages { get; private set; }

            public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
            {
                PageIndex = pageIndex;
                TotalPages = (int)Math.Ceiling(count / (double)pageSize);

                this.AddRange(items);
            }

            public bool HasPreviousPage => PageIndex > 1;
            public bool HasNextPage => PageIndex < TotalPages;

            public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
            {
                var count = await source.CountAsync();
                var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
                return new PaginatedList<T>(items, count, pageIndex, pageSize);
            }





        }



        public static async Task<Setting> ReadSettingAsync(DarouAppContext context)
        {
            if (context.Settings == null)
            {
                throw new InvalidOperationException("The Settings DbSet is not initialized.");
            }

            var setting = await context.Settings.FirstOrDefaultAsync();

            if (setting == null)
            {
                // Return a default setting if no record exists
                return new Setting
                {
                    DefaultColor = false,
                    NumberPerPage = 10 // Default value
                };
            }

            return setting;
        }




    }
}