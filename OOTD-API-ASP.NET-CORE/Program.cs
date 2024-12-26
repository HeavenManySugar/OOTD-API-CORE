using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


//using NSwagSample.Models;
// <snippet_Services>
using NSwag;
using NSwag.Generation.Processors.Security;
using OOTD_API.Models;
using OOTD_API.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

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
// </snippet_Services>

//builder.Services.AddDbContext<TodoContext>(options =>
//    options.UseInMemoryDatabase("Todo"));

builder.Services.AddControllers();
builder.Services.AddDbContext<Ootdv1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// <snippet_Middleware>
if (app.Environment.IsDevelopment())
{
    // Add OpenAPI 3.0 document serving middleware
    // Available at: http://localhost:<port>/swagger/v1/swagger.json
    app.UseOpenApi();

    // Add web UIs to interact with the document
    // Available at: http://localhost:<port>/swagger
    app.UseSwaggerUi(); // UseSwaggerUI is called only in Development.

    // Add ReDoc UI to interact with the document
    // Available at: http://localhost:<port>/redoc
    app.UseReDoc(options =>
    {
        options.Path = "/redoc";
    });
}
// </snippet_Middleware>

app.UseHttpsRedirection();

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