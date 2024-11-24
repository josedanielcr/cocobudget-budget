using System.Reflection;
using Carter;
using FluentValidation;
using web_api.Configurations;
using web_api.Extensions;

var builder = WebApplication.CreateBuilder(args);
Assembly assembly = typeof(Program).Assembly;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAppKeyVault(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAppCors();
builder.Services.AddAppCurrencyApi(builder.Configuration);
builder.Services.AddScoped<CurrencyExtension>();
builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(assembly));
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddCarter();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrations();
}

app.MapCarter();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.Run();