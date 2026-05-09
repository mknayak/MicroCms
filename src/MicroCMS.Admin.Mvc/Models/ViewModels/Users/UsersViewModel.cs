using MicroCMS.Admin.Mvc.Models.ApiDtos;
using System.ComponentModel.DataAnnotations;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Users;

public sealed class UsersViewModel
{
    public List<UserDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalPages { get; init; }
}

public sealed class InviteUserViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(100)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;
}
