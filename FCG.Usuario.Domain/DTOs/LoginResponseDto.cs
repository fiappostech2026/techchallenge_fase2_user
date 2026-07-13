using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.DTOs
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
    }
}
