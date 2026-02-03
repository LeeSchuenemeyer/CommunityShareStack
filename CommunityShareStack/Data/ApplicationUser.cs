using Microsoft.AspNetCore.Identity;

namespace CommunityShareStack.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Nickname { get; set; }
        public bool ShowNicknameOnly { get; set; }
        public string HomeAddress { get; set; }
        public string AstrologySign { get; set; }
        public bool AutoApproveEligible { get; set; }
    }
}
