using BlazorApp1.Data;
using BlazorApp1.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using BlazorApp1.Authentication;
using BlazorStrap;
using Syncfusion.Blazor;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;

    })
    .AddBootstrapProviders()
    .AddFontAwesomeIcons();
// Add services to the container.
builder.Services.AddAuthenticationCore();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<Emails>();
builder.Services.AddSingleton<Login>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationProvider>();
builder.Services.Configure<Settingsmodel>(
    builder.Configuration.GetSection("EmailDatabase"));
builder.Services.Configure<Settingsmodel_login>(
    builder.Configuration.GetSection("LoginDatabase"));
builder.Services.Configure<Settingsmodel_comments>(
	builder.Configuration.GetSection("CommentDatabase"));
builder.Services.Configure<Settingsmodel_solved>(
	builder.Configuration.GetSection("HistoryDatabase"));
builder.Services.Configure<Settingsmodel_newemails>(
	builder.Configuration.GetSection("NewEmailsDatabase"));
builder.Services.AddSyncfusionBlazor();





// Session storage
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
