using System.ComponentModel.DataAnnotations;
using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.ContentTypes;

public sealed class ContentTypeFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(100, ErrorMessage = "Display name must not exceed 100 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Handle is required.")]
    [RegularExpression(@"^[a-z][a-z0-9_]*$", ErrorMessage = "Handle must be lowercase alphanumeric with underscores.")]
    [StringLength(64, ErrorMessage = "Handle must not exceed 64 characters.")]
    [Display(Name = "Handle (API key)")]
    public string Handle { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Localization Mode")]
    public string LocalizationMode { get; set; } = "Single";

    [Display(Name = "Kind")]
    public string Kind { get; set; } = "Content";

    /// <summary>Current field definitions — populated when editing.</summary>
    public List<FieldDefinitionDto> Fields { get; set; } = [];

    public bool IsEdit => Id is not null;
}
