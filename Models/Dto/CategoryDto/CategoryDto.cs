using System.ComponentModel.DataAnnotations;

namespace FarmazonDemo.Models.Dto
{
    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        public int? DisplayOrder { get; set; }

        public bool? IsActive { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryResponseDto> SubCategories { get; set; } = new();
    }

    public class CategoryTreeDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }
}
