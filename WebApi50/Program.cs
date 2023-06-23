
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi50.Data;

namespace WebApi50
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<UserDbContext>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/users", async(UserDbContext db) =>
            {
                var users = await db.Users.ToListAsync();
                return Results.Ok(users);
            })
            .WithName("GetUsers")
            .WithOpenApi();

            app.MapPost("/users", async(User user, UserDbContext db) =>
            {
                var u = db.Users.Add(user);

                await db.SaveChangesAsync();
                return Results.Ok(u);
            })
            .WithName("AddUser")
            .WithOpenApi();

            app.MapPost("/Login", async(User user, UserDbContext db) =>
            {
                var userEncontrado = await db.Users.Where(x => x.Email==user.Email && x.Password==user.Password).FirstOrDefaultAsync();
            
                if (userEncontrado == null)
                {
                    return Results.Empty;
                }
                else
                {
                    // generate token
                    var token = CreateAccessToken(userEncontrado);

                    return Results.Ok(token);
                }
            })
            .WithName("Login")
            .WithOpenApi();

            app.Run();

            string CreateAccessToken(User user)
            {
                // suppose a public key can read from appsettings
                string K = "12345678901234567890123456789012345678901234567890123456789012345678901234567890";
                // convert to bytes
                var key = Encoding.UTF8.GetBytes(K);
                // convert to symetric Security
                var skey = new SymmetricSecurityKey(key);
                // Sign de Key
                var SignedCredential = new SigningCredentials(skey, SecurityAlgorithms.HmacSha256Signature);
                // Add Claims
                var uClaims = new ClaimsIdentity(new[]
                {     
                    new Claim(JwtRegisteredClaimNames.Sub,user.Name),     
                    new Claim(JwtRegisteredClaimNames.Email,user.Email) 
                });
                // define expiration
                var expires = DateTime.UtcNow.AddDays(1);
                // create de token descriptor
                var tokenDescriptor = new SecurityTokenDescriptor 
                { 
                    Subject = uClaims, 
                    Expires = expires, 
                    Issuer = "MasterBlazor", 
                    SigningCredentials = SignedCredential, 
                }; 
                //initiate the token handler
                var tokenHandler = new JwtSecurityTokenHandler(); 
                var tokenJwt = tokenHandler.CreateToken(tokenDescriptor); 
                var token = tokenHandler.WriteToken(tokenJwt);

                return token;
            }
        }
    }
}