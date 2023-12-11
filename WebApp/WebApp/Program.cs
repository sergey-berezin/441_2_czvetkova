using NuGetNN;
using System.Net;
using System.Text.Json.Serialization;
using WebApp.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Add services to the container.

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

//app.UseAuthorization();
app.UseCors(builder =>
{
    builder
    .WithOrigins("*")
    .WithHeaders("*")
    .WithMethods("*");

});

app.MapControllers();

app.Run();

public partial class Program
{

}
