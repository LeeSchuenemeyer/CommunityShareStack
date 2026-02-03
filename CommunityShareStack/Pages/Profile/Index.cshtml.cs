using System.Threading.Tasks;
using CommunityShareStack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CommunityShareStack.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            Input.FullName = user.FullName;
            Input.Nickname = user.Nickname;
            Input.ShowNicknameOnly = user.ShowNicknameOnly;
            Input.HomeAddress = user.HomeAddress;
            Input.MobilePhone = user.PhoneNumber;
            Input.AstrologySign = user.AstrologySign;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            user.FullName = Input.FullName;
            user.Nickname = Input.Nickname;
            user.ShowNicknameOnly = Input.ShowNicknameOnly;
            user.HomeAddress = Input.HomeAddress;
            user.PhoneNumber = Input.MobilePhone;
            user.AstrologySign = Input.AstrologySign;
            await _userManager.UpdateAsync(user);

            return RedirectToPage();
        }

        public class InputModel
        {
            public string FullName { get; set; }
            public string Nickname { get; set; }
            public bool ShowNicknameOnly { get; set; }
            public string HomeAddress { get; set; }
            public string MobilePhone { get; set; }
            public string AstrologySign { get; set; }
        }
    }
}
