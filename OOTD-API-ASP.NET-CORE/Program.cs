using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using OOTD_API.Models;
using OOTD_API.Security;
using OOTD_API.Services;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// 配置日誌層級
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning); // 設定為 Warning 或更高層級

builder.Services.AddScoped<IUserService, UserService>();
// 配置 JWT 驗證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var userId = context.Principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (userId == null || !await userService.IsUserEnabledAsync(userId))
                {
                    context.Fail("Unauthorized");
                }
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var errorResponse = new
                {
                    Status = false,
                    Message = "請重新登入"
                };
                return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
        };
    });

builder.Services.AddScoped<JwtAuthUtil>();

builder.Services.AddOpenApiDocument(doc =>
{
    doc.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        Description = "Type into the textbox: Bearer {your JWT token}.",
        In = OpenApiSecurityApiKeyLocation.Header,
        BearerFormat = "JWT",
        Scheme = JwtBearerDefaults.AuthenticationScheme // 不填寫會影響 Filter 判斷錯誤
    });
    doc.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));

    doc.PostProcess = document =>
    {
        document.Info = new OpenApiInfo
        {
            Version = "v1",
            Title = "OOTD API",
            Description = "An ASP.NET Core Web API",
            TermsOfService = "https://example.com/terms",
        };
    };
});

builder.Services.AddControllers().AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddDbContextPool<Ootdv1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure();
        sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    })
);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("OOTD-API-CORE");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseOpenApi();
    app.UseSwaggerUi(c =>
    {
        c.DocExpansion = "list";
    });
    app.UseReDoc(options =>
    {
        options.Path = "/redoc";
    });
}

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var serviceScope = app.Services.CreateScope())
{
    //var context = serviceScope.ServiceProvider.GetRequiredService<TodoContext>();

    //context.TodoItems.Add(new TodoItem { Name = "Item #1" });
    //await context.SaveChangesAsync();
}

app.Run();