// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Bookstore.Models.Identity;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace bookstore.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // we added to work with Identity Roles
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _uow;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, // we added to confirgure Identity Roles
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUnitOfWork uow
        )
        {
            _userManager = userManager;
            _roleManager = roleManager; // we added to confirgure Identity Roles
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _uow = uow;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            // *** BEGIN CUSTOM FIELDS WE WANT TO ADD FOR A USER
            public string Role { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; }

            [Required]
            public string Name { get; set; }
            public string StreetAddress { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
            // Note PhoneNumber is not a property in our ApplicationUser 
            // It's a field already in the User DB but we want to use it 
            //so we add it here as well with our other custom fields
            public string PhoneNumber { get; set; }
            public int? CompanyId { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem> CompanyList { get; set; }
            // *** END CUSTOM FIELDS WE WANT TO ADD FOR A USER
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            // ** BEGIN CUSTOM CODE TO ADD ROLES WHEN VISITING THE REGISTER PAGE
            var roles = new List<string> {
                SD.Role_Customer,
                SD.Role_Company,
                SD.Role_Admin,
                SD.Role_Employee,
            };

            var CreateRolesTask = roles
                .Where(r => !_roleManager.RoleExistsAsync(r).GetAwaiter().GetResult())
                .ToList();

            foreach (var task in CreateRolesTask)
            {
                await _roleManager.CreateAsync(new IdentityRole(task));
            }

            Input = new InputModel
            {
                RoleList =
                    _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    }),

                CompanyList = _uow.Companies.FromSql($@"
                    SELECT * FROM dbo.Companies;
                ", []).Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };
            // ** END CUSTOM CODE TO ADD ROLES WHEN VISITING THE REGISTER PAGE

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // *** BEGIN custom code to add custom fields to user db
                user.StreetAddress = Input.StreetAddress;
                user.City = Input.City;
                user.PostalCode = Input.PostalCode;
                user.State = Input.State;
                user.PhoneNumber = Input.PhoneNumber;
                user.Name = Input.Name;
                if (Input.Role == SD.Role_Company) user.CompanyId = Input.CompanyId;
                // *** END custom code to add custom fields to user db

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // *** BEGIN CUSTOM CODE TO ASSIGN A USER TO A ROLE WHEN THEY REGISTER
                    // *** FOR DEMO ONLY. USER CHOOSES ROLE WHEN REGISTERING. JUST FOR TESTING
                    // *** IN PRODUCTING USER SHOULD BE GIVEN A DEFALUT ROLE AND THEN ADMIN SHOULD
                    // *** THEN FURTHER ASSIGN FINAL ROLE
                    if (Input.Role is not null && Input.Role != "")
                    {
                        await _userManager.AddToRoleAsync(user, Input.Role);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                    }
                    // *** END CUSTOM CODE TO ASSIGN A USER TO A ROLE

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // ** BEGIN CUSTOM CODE TO ADD ROLES WHEN VISITING THE REGISTER PAGE
            Input = new InputModel
            {
                RoleList =
                    _roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Text = r.Name,
                        Value = r.Name
                    }),

                CompanyList = _uow.Companies.FromSql($@"
                    SELECT * FROM dbo.Companies;
                ", []).Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };
            // ** END CUSTOM CODE TO ADD ROLES WHEN VISITING THE REGISTER PAGE

            // If we got this far, something failed, redisplay form
            return Page();
        }

        // private IdentityUser CreateUser()
        private ApplicationUser CreateUser()
        {
            try
            {
                // return Activator.CreateInstance<IdentityUser>();
                return Activator.CreateInstance<ApplicationUser>(); // updated so we can customize info we collected on a user 
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
