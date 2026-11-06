using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.Models;

public static class Roles
{
    public const string Farmer = "Farmer";
    public const string Agronomist = "Agronomist";
    public const string Admin = "Admin";
}

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = Roles.Farmer;
}
