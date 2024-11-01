using Microsoft.Extensions.DependencyInjection;
using Application.Services;
using SharedKernel.Interfaces;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IRedditService, RedditService>();
builder.Services.AddScoped<IRedditApiService, RedditApiService>();
builder.Services.AddScoped<IRedditStatsService, RedditStatsService>();


builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseEndpoints(endpoints => 
{
    _ = endpoints.MapControllers();
});
app.MapControllers();



app.Run();
