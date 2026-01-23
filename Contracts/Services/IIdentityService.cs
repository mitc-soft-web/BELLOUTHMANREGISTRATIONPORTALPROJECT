using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mitc_Registration_Portal.Models.Entities;

namespace Mitc_Registration_Portal.Contracts.Services
{
    public interface IIdentityService
    {
        string GetUserIdentity();

        string GenerateToken(User user, IEnumerable<string> roles);
        public IEnumerable<Claim> ValidateToken(string jwtToken);

        JwtSecurityToken GetClaims(string token);

        string GetClaimValue(string type);

        string GenerateSalt();

        public string GetPasswordHash(string password, string salt = null);
        Task<User> FindByNameAsync(string userName);
        Task<User> FindUserAsync(string userName);
        Task<IList<string>> GetRolesAsync(User user);
        //bool CheckPasswordAsync(User user, string password);
        public Task<User> GetLoggedInUser();
    }
}
