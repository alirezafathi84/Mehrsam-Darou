using Mehrsam_Darou.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Mehrsam_Darou.Helper.Helper;

namespace Mehrsam_Darou.Controllers
{
    public class UserController : BaseController
    {
        private readonly ILogger<UserController> _logger;
        private readonly DarouAppContext _context;

        // Combine both constructors into one
        public UserController(DarouAppContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> UserList(int? page, string SearchKey)
        {




            // Set common view data and get the page size
            var setting = await ReadSettingAsync(_context);
            int pageSize = Convert.ToInt32(setting.NumberPerPage ?? 10); // Default to 10 if setting.NumberPerPage is null
            int pageNumber = page ?? 1;

            // Base query for fetching users
            IQueryable<User> query = _context.Users.Include(u => u.Team);

            // Apply search filter if SearchKey is provided
            if (!string.IsNullOrWhiteSpace(SearchKey))
            {
                query = query.Where(u => u.Username.Contains(SearchKey) || u.FirstName.Contains(SearchKey) || u.LastName.Contains(SearchKey)).OrderBy(e => e.DateCreated);
            }

            // Get total count after filtering
            int totalUsers = await query.CountAsync();

            // Fetch paginated results
            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the paginated list
            var paginatedUsers = new PaginatedList<User>(users, totalUsers, pageNumber, pageSize);

            ViewBag.Teams = await _context.Teams.ToListAsync();

            // Pass paginated list to the view
            return View(paginatedUsers);
        }


        // Action to display a specific user's details by their ID
        [HttpGet("User/UserDetails/{id}")]
        public async Task<IActionResult> UserDetails(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                ViewData["Error"] = "رکوردی یافت نشد";
                return View(); // Return view with an error message
            }

            return View(user);  // Pass the user object as the model to the view
        }


        public async Task<IActionResult> AddNewUser()
        {


            // Set common view data and get the page size
            //  var setting = await ReadSettingAsync(_context);

            var Teams = await _context.Teams.OrderBy(e => e.Name).ToListAsync();

            return View("AddUser", Teams);
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User user, string ConfirmPassword, Guid TeamDDL, IFormFile AvatarImg)
        {



            // Check if the password and confirm password match
            if (user.Password != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "رمز عبور و تایید آن یکسان نیستند.";
                return View("AddUser");
            }

            // Check if the username already exists
            bool isDuplicate = await _context.Users.AnyAsync(u => u.Username == user.Username);

            if (isDuplicate)
            {
                TempData["ErrorMessage"] = "نام کاربری قبلاً ثبت شده است.";
                return View("AddUser");
            }

            // Hash the password before saving it
            user.Password = Helper.Helper.HashPassword(user.Password);

            // Set the DateCreated to the current date and time
            user.DateCreated = DateTime.Now;
            user.DateModified = DateTime.Now;
            // Handle Avatar Upload
            if (AvatarImg != null && AvatarImg.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarImg.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/users/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarImg.CopyToAsync(stream);
                }

                user.AvatarImg = @"\images\users\" + fileName; // Save the file name (not the full path) to the database
            }
            else
            {
                user.AvatarImg = null;  // Keep the old avatar if no new one is uploaded
            }
            user.TeamId = TeamDDL;

            // Add the user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "کاربر با موفقیت اضافه شد.";
            return RedirectToAction("UserList");
        }





        [HttpGet]
        public async Task<IActionResult> EditUser(Guid id)
        {            // Validate session and get the user


            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var teams = await _context.Teams.ToListAsync();
            ViewBag.User = user;
            return View(teams);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user, string ConfirmPassword, Guid TeamDDL, IFormFile AvatarImg)
        {
            if (!string.IsNullOrEmpty(user.Password) && user.Password != ConfirmPassword)
            {
                var teams = await _context.Teams.ToListAsync();
                ViewBag.User = user;
                TempData["ErrorMessage"] = "رمز عبور و تایید آن یکسان نیستند.";
                return View("EditUser", teams);
            }

            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "کاربر مورد نظر یافت نشد.";
                return View("EditUser");
            }

            _context.Entry(existingUser).State = EntityState.Detached;

            if (!string.IsNullOrEmpty(user.Password))
            {
                user.Password = HashPassword(user.Password); // Assuming HashPassword is your method for hashing
            }
            else
            {
                user.Password = existingUser.Password; // Keep the old password if not changed
            }

            // Handle Avatar Upload
            if (AvatarImg != null && AvatarImg.Length > 0)
            {
                // Delete the previous image if it exists
                if (!string.IsNullOrEmpty(existingUser.AvatarImg))
                {
                    var previousImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingUser.AvatarImg.TrimStart('\\'));
                    if (System.IO.File.Exists(previousImagePath))
                    {
                        System.IO.File.Delete(previousImagePath);
                    }
                }

                // Save the new image
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarImg.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/users/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarImg.CopyToAsync(stream);
                }

                user.AvatarImg = @"\images\users\" + fileName; // Save the file path to the database
            }
            else
            {
                // Keep the old avatar if no new one is uploaded
                user.AvatarImg = existingUser.AvatarImg;
            }

            user.DateCreated = existingUser.DateCreated;
            user.DateModified = DateTime.Now;
            user.Version = existingUser.Version + 1;
            user.TeamId = TeamDDL; // Set the team ID from the dropdown
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "اطلاعات کاربر با موفقیت به‌روزرسانی شد.";
            return RedirectToAction("UserList");
        }




        [HttpPost]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "کاربر مورد نظر یافت نشد.";
                return RedirectToAction("UserList");
            }

            // Delete the user's image if it exists
            if (!string.IsNullOrEmpty(user.AvatarImg))
            {
                // Construct the full path to the image file
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarImg.TrimStart('\\'));

                // Check if the file exists and delete it
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Users.Remove(user);  // Remove the user from the database
            await _context.SaveChangesAsync();  // Save the changes to the database

            TempData["SuccessMessage"] = "کاربر با موفقیت حذف شد.";
            return RedirectToAction("UserList");
        }





    }



}
