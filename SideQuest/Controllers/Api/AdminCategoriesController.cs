using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideQuest.Authorization;
using SideQuest.Contracts;
using SideQuest.Services;

namespace SideQuest.Controllers.Api
{
    [Authorize(Roles = SideQuestRoles.Admin)]
    [Route("api/admin/categories")]
    public class AdminCategoriesController : ApiControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminCategoriesController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> Get()
        {
            return ToActionResult(await _adminService.GetCategoriesAsync());
        }

        [HttpPost]
        public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
        {
            return ToActionResult(await _adminService.CreateCategoryAsync(request));
        }

        [HttpPut("{categoryId:int}")]
        public async Task<ActionResult<CategoryResponse>> Update(int categoryId, CategoryRequest request)
        {
            return ToActionResult(await _adminService.UpdateCategoryAsync(categoryId, request));
        }
    }
}
