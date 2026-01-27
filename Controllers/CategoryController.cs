using FarmazonDemo.Models.Dto;
using FarmazonDemo.Services.Categories;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Get all active categories
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
        {
            var categories = await _categoryService.GetAllAsync(includeInactive: false);
            return Ok(categories);
        }

        /// <summary>
        /// Get category tree structure
        /// </summary>
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<CategoryTreeDto>>> GetTree()
        {
            var tree = await _categoryService.GetTreeAsync();
            return Ok(tree);
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
        /// Get category by slug
        /// </summary>
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CategoryResponseDto>> GetBySlug(string slug)
        {
            var category = await _categoryService.GetBySlugAsync(slug);
            if (category == null)
                return NotFound();
            return Ok(category);
        }

        /// <summary>
        /// Get subcategories
        /// </summary>
        [HttpGet("{id}/subcategories")]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetSubCategories(int id)
        {
            var categories = await _categoryService.GetSubCategoriesAsync(id);
            return Ok(categories);
        }
    }
}
