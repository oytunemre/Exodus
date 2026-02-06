using Exodus.Models.Dto;
using Exodus.Services.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Exodus.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminCategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public AdminCategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Get all categories including inactive (Admin)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll([FromQuery] bool includeInactive = true)
        {
            var categories = await _categoryService.GetAllAsync(includeInactive);
            return Ok(categories);
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CreateCategoryDto dto)
        {
            var category = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound();
            return Ok(category);
        }

        /// <summary>
        /// Update category
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var category = await _categoryService.UpdateAsync(id, dto);
            return Ok(category);
        }

        /// <summary>
        /// Delete category
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Toggle category active status
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<ActionResult<CategoryResponseDto>> ToggleActive(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            var updated = await _categoryService.UpdateAsync(id, new UpdateCategoryDto
            {
                IsActive = !category.IsActive
            });

            return Ok(updated);
        }
    }
}
