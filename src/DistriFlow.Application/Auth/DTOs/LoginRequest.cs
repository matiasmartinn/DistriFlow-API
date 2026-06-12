using System.ComponentModel.DataAnnotations;

namespace DistriFlow.Application.Auth.DTOs;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "El email es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido.")]
    public string Email { get; init; } = null!;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; init; } = null!;
}
