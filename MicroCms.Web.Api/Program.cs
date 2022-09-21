using MicroCms.Core;
using MicroCms.Pacakge;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var configOption = builder.Configuration.GetSection(MicroCmsConfigurationOption.ConfigSectionKey)
                                                     .Get<MicroCmsConfigurationOption>();

builder.Services.AddMicroCms(configOption).AddPackageFeature();
builder.Services.AddRouting(route => route.LowercaseUrls = true);
builder.Services.AddControllers();
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

app.MapControllers();
app.InitializeMicroCms();
app.AddSamplePackages();

app.Run();
