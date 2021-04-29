using BookShop.Areas.Identity.Data;
using BookShop.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShop.Models.Repository
{
    public interface IUsersRepository
    {
        Task<IdentityResult> RegisterAsync(RegisterBaseViewModel ViewModel);
        Task<IdentityResult> AddRoleToUser(string name, ApplicationUser user);
    }
}
