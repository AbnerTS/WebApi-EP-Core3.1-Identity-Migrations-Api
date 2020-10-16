using System.Text;
using DevIO.Api.Data;
using DevIO.Api.Extensions;
using DevIO.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DevIO.Api.Configuration
{
    public static class IdentityConfig
    {
        public static IServiceCollection AddIdentityConfig(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            //adiciono o Identity para uso na aplicação, feito após ter instalado o banco.
            //Instalar o seguinte pacote: install-package  Microsoft.AspNetCore.Identity.UI, para identificar o AddDefaultIdentity
            services.AddDefaultIdentity<IdentityUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddErrorDescriber<IdentityMensagensPortugues>() //Traduz erros do Identity
                .AddDefaultTokenProviders();

            // JWT - Implementação do Jason Web Token

            //Configura o AppSettings
            var appSettingsSection = configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            //Pego os dados da classe AppSettings
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret); //Encoding do segredo configurado no AppSettings.Json

            //Adiciono uma autenticação com as seguintes configurações
            //Instalar o pacote: Microsoft.AspNetCore.Authentication.JwtBearerDefaults
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false; //Qdo true garante que só será usado https
                x.SaveToken = true; //Guarda o token no http Auhentication property
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, //Valida quem emitiu o token, com base na chave que passamos
                    IssuerSigningKey = new SymmetricSecurityKey(key), //Nossa chave criptografada
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = appSettings.ValidoEm,
                    ValidIssuer = appSettings.Emissor
                };
            });

            return services;
        }
    }
}