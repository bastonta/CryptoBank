using System.Reflection;
using CryptoBank.WebApi.Data;
using CryptoBank.WebApi.Errors.Extensions;
using CryptoBank.WebApi.Features.Account.Registration;
using CryptoBank.WebApi.Features.Identity.Registration;
using CryptoBank.WebApi.Features.News.Registration;
using CryptoBank.WebApi.Pipeline.Behaviors;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
);

// Add services to the container.
builder.Services.AddFastEndpoints();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.SwaggerDocument();

builder.AddNews();
builder.AddIdentity();
builder.AddAccounts();


var app = builder.Build();

app.MapProblemDetails();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
    app.UseSwaggerUi3();
}

app.Run();

public partial class Program {}
