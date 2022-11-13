using System.Text;
using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<ITokenService,TokenService>();
builder.Services.AddScoped<IUserRepository,UserRepository>();
                builder.Services.AddDbContext<DataContext>(option=>{
                     option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                    });

builder.Services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options=>{
    options.TokenValidationParameters=new   TokenValidationParameters{
        ValidateIssuerSigningKey=true,
        IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["TokenKey"])),
        ValidateIssuer=false,
        ValidateAudience=false
    
    } ;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors(x=>x.AllowAnyHeader().AllowAnyMethod().WithOrigins("https://localhost:4200"));
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<DataContext>();
    
    await context.Database.MigrateAsync();
    await Seed.SeedUsers(context);

     
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration");
}

await app.RunAsync();
