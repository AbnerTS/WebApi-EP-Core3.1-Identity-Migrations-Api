using DevIO.Api.Controllers;
using DevIO.Api.Extensions;
using DevIO.Api.ViewModels;
using DevIO.Business.Intefaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DevIO.API.V1.Controllers
{
    //[ApiVersion("2.0")]
    //[ApiVersion("1.0", Deprecated = true)] //Indica via header que a API esta obsoleta
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}")]
    public class AuthController : MainController
    {
        //Injetado do Identity
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        //JWT
        private readonly AppSettings _appSettings;

        public AuthController(INotificador notificador,
                              SignInManager<IdentityUser> signInManager,
                              IOptions<AppSettings> appSettings,
                              UserManager<IdentityUser> userManager,
                              IUser user) : base(notificador, user)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _appSettings = appSettings.Value;
        }

        [HttpPost("nova-conta")]
        public async Task<ActionResult> Registrar(RegisterUserViewModel registerUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new IdentityUser
            {
                UserName = registerUser.Email,
                Email = registerUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                //return CustomResponse(GerarJWT()); //Usando token
                return CustomResponse(await GerarJwt(user.Email)); //Usando token com claims
                //return CustomResponse(registerUser); //Sem Token.
            }
            foreach (var error in result.Errors)
            {
                NotificarErro(error.Description);
            }

            return CustomResponse(registerUser);
        }

        [HttpPost("entrar")]
        public async Task<ActionResult> Login(LoginUserViewModel loginUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            // 3º parametro se é persistente e o 4º se vc irá bloquear por alguns minutos em caso de 5 falhas seguidas
            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                return CustomResponse(await GerarJwt(loginUser.Email)); //Usando token com claims
                //Usando Token
                //return CustomResponse(GerarJWT());
                //Sem token
                //return CustomResponse(loginUser);
            }

            //Se o usuário estiver bloqueado
            if (result.IsLockedOut)
            {
                NotificarErro("Usuário temporariamente bloqueado por tentativas inválidas");
                return CustomResponse(loginUser);
            }

            NotificarErro("Usuário ou Senha incorretos");

            return CustomResponse(loginUser);
        }

        /// <summary>
        /// Gerar Json Web Token sem utilizar Claims
        /// </summary>
        /// <returns>String token</returns>
        private string GerarJWT()
        {
            //JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Emissor,
                Audience = _appSettings.ValidoEm,
                //Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token);
            return encodedToken;

        }

        /// Gerar Json Web Token utilizando Claims
        /// </summary>
        /// <param name="email">e-mail</param>
        /// <returns>Retorna um LoginResponseViewModel com dados do token e claims populado, dentro do nó data do JWT</returns>
        private async Task<LoginResponseViewModel> GerarJwt(string email)
        {
            //Claims
            var user = await _userManager.FindByEmailAsync(email); //Obtem o usuário
            var claims = await _userManager.GetClaimsAsync(user); //Obtem as claims do usuário
            var userRoles = await _userManager.GetRolesAsync(user); //Obtem as regras

            //Adiciono as claims do usuário as claims do token
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            //Adiciono as regras as claims
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("role", userRole));
            }

            //Converto para o tipo ClaimsIdentity
            var identityClaims = new ClaimsIdentity();
            //Adiciono a coleção de claims
            identityClaims.AddClaims(claims);
            //---------------------------------------

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Emissor,
                Audience = _appSettings.ValidoEm,
                Subject = identityClaims, //Adicono ao token as clains criadas
                Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });

            var encodedToken = tokenHandler.WriteToken(token);

            // Populo LoginResponseViewModel com dados para o front-end
            var response = new LoginResponseViewModel
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(_appSettings.ExpiracaoHoras).TotalSeconds,
                UserToken = new UserTokenViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Claims = claims.Select(c => new ClaimViewModel { Type = c.Type, Value = c.Value })
                }
            };

            return response;
        }

        /// <summary>
        /// Gerar Json Web Token utilizando Claims
        /// </summary>
        /// <param name="email">e-mail</param>
        /// <returns>String token</returns>
        //private async Task<string> GerarJwt(string email)
        //{
        //    //Claims
        //    var user = await _userManager.FindByEmailAsync(email); //Obtem o usuário
        //    var claims = await _userManager.GetClaimsAsync(user); //Obtem as claims do usuário
        //    var userRoles = await _userManager.GetRolesAsync(user); //Obtem as regras

        //    //Adiciono as claims do usuário as claims do token
        //    claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
        //    claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        //    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        //    claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
        //    claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

        //    //Adiciono as regras as claims
        //    foreach (var userRole in userRoles)
        //    {
        //        claims.Add(new Claim("role", userRole));
        //    }

        //    //Converto para o tipo ClaimsIdentity
        //    var identityClaims = new ClaimsIdentity();
        //    //Adiciono a coleção de claims
        //    identityClaims.AddClaims(claims);
        //    //---------------------------------------

        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
        //    var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        //    {
        //        Issuer = _appSettings.Emissor,
        //        Audience = _appSettings.ValidoEm,
        //        Subject = identityClaims, //Adicono ao token as clains criadas
        //        Expires = DateTime.UtcNow.AddHours(_appSettings.ExpiracaoHoras),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    });

        //    var encodedToken = tokenHandler.WriteToken(token);

        //    return encodedToken;

        //}



        /// <summary>
        /// Método para formatar data no padrão UnixEpochDate
        /// </summary>
        /// <param name="date">date</param>
        /// <returns>Segundos relativo a data que está sendo passada</returns>
        private static long ToUnixEpochDate(DateTime date)
           => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
