using Gym_memrship_Managment.Models;
using Microsoft.AspNetCore.Identity;

namespace Gym_memrship_Managment.Security
{
    public class BCryptPasswordHasher : IPasswordHasher<ApplicationUser>
    {
        public string HashPassword(ApplicationUser user, string password)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, 11);
        }

        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword)) return PasswordVerificationResult.Failed;

            // Check if the hash is a PBKDF2 hash (Identity default)
            if (hashedPassword.Contains("AQAAAA") || hashedPassword.Length > 60) 
            {
                // Fallback to default Identity hasher to verify legacy passwords
                var defaultHasher = new PasswordHasher<ApplicationUser>();
                var legacyResult = defaultHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                
                if (legacyResult == PasswordVerificationResult.Success)
                {
                    return PasswordVerificationResult.SuccessRehashNeeded;
                }
                return legacyResult;
            }

            // Verify as BCrypt
            try
            {
                if (BCrypt.Net.BCrypt.EnhancedVerify(providedPassword, hashedPassword))
                {
                    return PasswordVerificationResult.Success;
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return PasswordVerificationResult.Failed;
            }

            return PasswordVerificationResult.Failed;
        }
    }
}
