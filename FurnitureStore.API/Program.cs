using System.Text;
using FurnitureStore.API.Configuration;
using FurnitureStore.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",new OpenApiInfo
    {
        Title = "Furniture_Store_API",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter prefix Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
        }
    });
});
builder.Services.AddDbContext<FurnitureStoreContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("FurnitureStoreContext"));
});

builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwt =>
{
    var key = Encoding.ASCII.GetBytes((builder.Configuration.GetSection("JwtConfig:Secret").Value));
    jwt.SaveToken = true; //almacena el token si la auth es exitosa
    jwt.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true, //siempre validar el token
        IssuerSigningKey = new SymmetricSecurityKey(key), //esta es la key de la validacion de la linea de arriba,
        ValidateIssuer = false, //solo en dev, en prod es true
        ValidateAudience =
            false, //solo en dev, en prod es true hay que validar quien era el destinatario, no puede usar el token en ningun otro lado
        RequireExpirationTime = false, //falso hasta hacer el refresh token
        ValidateLifetime = true //validara el tiempo de vida
    };
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>options.SignIn.RequireConfirmedAccount =false)
    .AddEntityFrameworkStores<FurnitureStoreContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
