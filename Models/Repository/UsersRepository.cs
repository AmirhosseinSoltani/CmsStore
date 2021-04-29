using BookShop.Areas.Identity.Data;
using BookShop.Classes;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShop.Models.Repository
{
    public class UsersRepository :IUsersRepository
    {
        private readonly IApplicationUserManager _userManager;
        private readonly IApplicationRoleManager _roleManager;
        private readonly IConvertDate _convertDate;
        public UsersRepository(IApplicationUserManager userManager, IApplicationRoleManager roleManager, IConvertDate convertDate)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _convertDate = convertDate;
        }


        public async Task<IdentityResult> RegisterAsync(RegisterBaseViewModel ViewModel)
        {
            DateTime BirthDateMiladi = _convertDate.ConvertShamsiToMiladi(ViewModel.BirthDate);
            var user = new ApplicationUser { UserName = ViewModel.UserName, Email = ViewModel.Email, PhoneNumber = ViewModel.PhoneNumber, RegisterDate = DateTime.Now, IsActive = true, BirthDate = BirthDateMiladi, EmailConfirmed = ViewModel.EmailConfirmed, PhoneNumberConfirmed = ViewModel.PhoneNumberConfirmed };
            IdentityResult result = await _userManager.CreateAsync(user, ViewModel.Password);
            if (result.Succeeded)
                return await AddRoleToUser("کاربر", user);
            else
                return result;
        }

        public async Task<IdentityResult> AddRoleToUser(string name,ApplicationUser user)
        {
            var role = _roleManager.FindByNameAsync(name);
            if (role == null)
                await _roleManager.CreateAsync(new ApplicationRole(name));

            return await _userManager.AddToRoleAsync(user, name);
        }
    }
}
