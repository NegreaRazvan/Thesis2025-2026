using Controller.Interfaces;
using Controller.Middlewares;
using Repository;
using Repository.AutoMapper;
using Repository.Context;
using Repository.DataSeeder;
using Repository.EFEntities;
using Service.Interfaces;
using Service.Services;
using Service.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Controller.Controllers.AuthController).Assembly);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LinguaForge API",
        Version = "v1",
        Description = "German CEFR Trainer — AI-powered writing analysis, grammar feedback, flashcards, and coaching.",
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, "Controller.xml");
    if (File.Exists(xmlFile)) c.IncludeXmlComments(xmlFile);

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
        Array.Empty<string>()
    }});
});

const string AllowedOrigins = "_allowedOrigins";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(name: AllowedOrigins, policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDbContext<GermanAIContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<GermanAIContext>()
.AddDefaultTokenProviders();

if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IPasswordHasher<AppUser>, FastPasswordHasher<AppUser>>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy("expensive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});
builder.Services.AddAutoMapper(typeof(EFEntitiesMappingProfile).Assembly);
builder.Services.AddScoped<IAppValidatorFactory, ValidatorFactory>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterPostDTOValidator>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<IFlashcardRepository, FlashcardRepository>();
builder.Services.AddScoped<IWritingPromptRepository, WritingPromptRepository>();
builder.Services.AddScoped<IFlashcardSetRepository, FlashcardSetRepository>();
builder.Services.AddScoped<IVocabGameRepository, VocabGameRepository>();
builder.Services.AddScoped<ICoachSessionRepository, CoachSessionRepository>();

builder.Services.AddHttpClient<IPythonMlService, PythonMlService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PythonApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddScoped<IAiFeedbackService, AiFeedbackService>();
builder.Services.AddHttpClient("PythonProxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITranscriptionService, TranscriptionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IFlashcardService, FlashcardService>();
builder.Services.AddScoped<IWritingPromptService, WritingPromptService>();
builder.Services.AddScoped<IFlashcardSetService, FlashcardSetService>();
builder.Services.AddScoped<ICoachService, CoachService>();

builder.Services.AddHttpClient<IVocabGameService, VocabGameService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PythonApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddScoped<WritingPromptSeeder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GermanAIContext>();
    db.Database.Migrate();
    var seeder = scope.ServiceProvider.GetRequiredService<WritingPromptSeeder>();
    await seeder.SeedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors(AllowedOrigins);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();