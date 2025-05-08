using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using NCM.Data;
using Microsoft.AspNetCore.DataProtection;
using NCM.Controllers;
using NCM.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. EF Core: đăng ký ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Data Protection: lưu key vào thư mục SSH (theo cấu hình hoặc fallback)
var keyFolder = builder.Configuration["DataProtection:KeyFolder"];
if (string.IsNullOrEmpty(keyFolder))
{
    keyFolder = "SSH";
}
var keyPath = Path.Combine(builder.Environment.ContentRootPath, keyFolder);
Directory.CreateDirectory(keyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("NCMApp");

// 3. MVC: Controllers with Views
builder.Services.AddControllersWithViews();

// 4. Background Service: scheduler backup & compliance
builder.Services.AddHostedService<BackupComplianceService>();

// 5. Health Checks: EF Core database
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("Database");
builder.Services.AddHostedService<BackupComplianceService>();
var app = builder.Build();

// 6. Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

// 7. Endpoints
app.MapHealthChecks("/healthz");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Device}/{action=Index}/{id?}");

app.Run();
