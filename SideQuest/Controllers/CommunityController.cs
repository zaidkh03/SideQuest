using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideQuest.Authorization;
using SideQuest.Data;
using SideQuest.Models;
using SideQuest.ViewModels;

namespace SideQuest.Controllers
{
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            SetCommunityViewData("Community");

            var posts = await _context.CommunityPosts
                .AsNoTracking()
                .Include(post => post.User)
                .Include(post => post.Comments)
                .OrderByDescending(post => post.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(new CommunityIndexViewModel
            {
                Posts = posts.Select(ToPostCard).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityPostFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Add a title and post content before publishing.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            _context.CommunityPosts.Add(new CommunityPost
            {
                UserId = userId,
                Title = form.Title.Trim(),
                Content = form.Content.Trim(),
                Type = form.Type,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Post published.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            SetCommunityViewData("Community");

            var post = await _context.CommunityPosts
                .AsNoTracking()
                .Include(existingPost => existingPost.User)
                .Include(existingPost => existingPost.Comments)
                    .ThenInclude(comment => comment.User)
                .FirstOrDefaultAsync(existingPost => existingPost.Id == id);

            if (post is null)
            {
                return NotFound();
            }

            return View(new CommunityPostDetailViewModel
            {
                Post = ToPostCard(post),
                Content = post.Content,
                Comments = post.Comments
                    .OrderBy(comment => comment.CreatedAt)
                    .Select(comment => new CommunityCommentViewModel
                    {
                        AuthorName = PortalPageMapping.DisplayName(comment.User),
                        Content = comment.Content,
                        CreatedAt = comment.CreatedAt
                    })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int id, CommunityCommentFormViewModel form)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Write a comment before posting.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (!await _context.CommunityPosts.AnyAsync(post => post.Id == id))
            {
                return NotFound();
            }

            _context.CommunityComments.Add(new CommunityComment
            {
                PostId = id,
                UserId = userId,
                Content = form.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Comment added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private static CommunityPostCardViewModel ToPostCard(CommunityPost post)
        {
            var preview = post.Content.Length > 180 ? $"{post.Content[..180]}..." : post.Content;
            return new CommunityPostCardViewModel
            {
                Id = post.Id,
                Title = post.Title,
                ContentPreview = preview,
                AuthorName = PortalPageMapping.DisplayName(post.User),
                Type = post.Type,
                CommentCount = post.Comments.Count,
                CreatedAt = post.CreatedAt
            };
        }

        private void SetCommunityViewData(string activeNav)
        {
            ViewData["Title"] = "Community";
            ViewData["TopBarTitle"] = "Community";
            ViewData["Shell"] = "App";
            ViewData["ActivePortal"] = User.IsInRole(SideQuestRoles.Admin)
                ? "Admin"
                : User.IsInRole(SideQuestRoles.Employer)
                    ? "Employer"
                    : User.IsInRole(SideQuestRoles.Worker)
                        ? "Worker"
                        : "Onboarding";
            ViewData["ActiveNav"] = activeNav;
        }
    }
}
