using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages()
    .AddViewLocalization()           // for cshtml views
    .AddDataAnnotationsLocalization(); // for validation messages;

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("fr"),
    };
    opts.DefaultRequestCulture = new RequestCulture("en");
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;

    // Optional: culture via query string, cookie, Accept-Lang header
    opts.RequestCultureProviders.Insert(0,
       new CookieRequestCultureProvider()
       {
           CookieName = CookieRequestCultureProvider.DefaultCookieName,
       });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(locOptions);

app.UseRouting();

app.UseAuthorization();
app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
