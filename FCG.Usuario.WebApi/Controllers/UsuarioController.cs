using FCG.Usuario.Domain.Dto;
using FCG.Usuario.Domain.DTOs;
using FCG.Usuario.Domain.Interfaces;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Usuario.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly IValidator<CriarUsuarioDto> _validator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<UsuarioController> _logger;

        public UsuarioController(
            IUsuarioService service,
            IValidator<CriarUsuarioDto> validator,
            IPublishEndpoint publishEndpoint,
            ILogger<UsuarioController> logger)
        {
            _service = service;
            _validator = validator;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpPost("criar")]
        public async Task<IActionResult> CriarUsuario([FromBody] CriarUsuarioDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

                var id = await _service.CriarUsuarioAsync(dto);

                if (id is null)
                    return BadRequest("Usuário já cadastrado.");

                await _publishEndpoint.Publish(new UserCreatedEvent
                {
                    UserId = id.Value,
                    Email = dto.Email
                });

                _logger.LogInformation("UserCreatedEvent publicado para o usuário {UserId}", id.Value);

                return CreatedAtAction(nameof(CriarUsuario), new { id }, new { id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]       
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _service.LoginAsync(dto);

                if (token is null)
                    return Unauthorized("E-mail ou senha inválidos.");

                return Ok(token);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
