namespace OzelDers.Business.DTOs;

// Kullanıcı bilgilerini dışarıya taşıyan DTO
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsTeacherProfileComplete { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int TokenBalance { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Kayıt formu
public class UserRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

// Giriş formu
public class UserLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// JWT sonucu
public class AuthResultDto
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
}

// Profil güncelleme formu
public class UserProfileUpdateDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Phone { get; set; }
}

// Refresh token isteği
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
