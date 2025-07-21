#pragma warning disable CA1515

using System.ComponentModel.DataAnnotations;

namespace Stickerlandia.UserManagement.Api.ViewModels;

public sealed class AuthorizeViewModel
{
    [Display(Name = "Application")]
    public string? ApplicationName { get; set; }

    [Display(Name = "Scope")]
    public string? Scope { get; set; }
}