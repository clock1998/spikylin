---
title : Resturcture Clean Architecture to Vertical Slice Arhcitecture Part 2
description:  Refactory a Clean Architecture project to a Vertical Slice Arhcitecture project, change repository pattern to CQRS.
date: '2025-02-02'
tags: 
    - ASP.NET
    - Architecture
    - Refactoring
published: true
featured: false
---

Add-Migration SomeMigration -OutputDir Data\Migrations

I dicided to change the architecture from Clean Architecture to CQRS. There are a few reasons:
1. Remove abstraction.
2. Increase readability.
3. Feature based file structure.
4. Reduce merge conflict.

In my old clean architecture, all the business logic resides in the AuthRepository.cs file. But now, the logic is seperated into different commands:
![alt text](/post_images/refactor-clean-architecture-to-vertical-slice/CQRS-file-structure.png)
However, I did not put my controller methods in the handlers, as I use traditional controller methods. I feel it is better to put all the end points in the same class so I can get the grouping and naming feature from the framework. One downside is that if I have many more end points for a single feature, the controller class will be super large. A potential solution is to have partial class. Minimal API method is another option, and it is better to use Minimal API if you want to put end points in the same files as handlers.

I also added Fluent Validator. It makes the validation more clean. I mainly use the validator to valide data model not business logic validation.

Here is the login handler.
```csharp
namespace WebAPI.Features.Auth.Query
{
    public sealed record LoginRequest(string Username, string Password);

    public sealed class LoginValidator : AbstractValidator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(n => n.Username).NotEmpty().EmailAddress();
            RuleFor(n => n.Password).NotEmpty();
        }
    }

    public class LoginHandler
    {
        private readonly AuthHandler _authHandler;
        public LoginHandler(AuthHandler authHandler)
        {
            _authHandler = authHandler;
        }

        public async Task<AppContextResponse> HandleAsync(LoginRequest request)
        {
            var user = await _authHandler.UserManager.FindByEmailAsync(request.Username);
            if (user != null)
            {
                var result = await _authHandler.UserManager.CheckPasswordAsync(user, request.Password);
                var isEmailConfirmed = await _authHandler.UserManager.IsEmailConfirmedAsync(user);
                if (result)
                {
                    if (isEmailConfirmed)
                    {
                        user.RefreshToken = AuthHelper.CreateRefreshToken();
                        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(24);
                        await _authHandler.UserManager.UpdateAsync(user);
                        var roles = await _authHandler.UserManager.GetRolesAsync(user);
                        if (roles.Any())
                        {
                            var response = new AppContextResponse
                            {
                                Token = AuthHelper.CreateToken(user, roles.ToList(), _authHandler.Configuration),
                                RefreshToken = user.RefreshToken,
                                User = new UserReponse
                                {
                                    Id = user.Id.ToString(),
                                    Email = user.Email!,
                                    FirstName = user.UserProfile.FirstName,
                                    LastName = user.UserProfile.LastName,
                                    Roles = user.UserRoles.Where(n => n.Role.Name != null).Select(n => n.Role.Name!).ToList(),
                                },
                            };
                            return response;
                        }
                        throw new InvalidOperationException("The user does not have a role.");
                    }
                    throw new InvalidOperationException("The user's email is not confirmed.");
                }
            }
            throw new InvalidOperationException("Username or password incorrect.");
        }
    }
}
```
Here is the login controller method.
```csharp
[Route("api/[controller]")]
[ApiController]
public abstract class AuthController(
    RegisterHandler _registerHandler, IValidator<RegisterRequest> _registerValidator,
    VerifyEmailHander _verifyEmailhandler, IValidator<VerifyEmailRequest> _verifyEmailValidator,
    LoginHandler _loginHandler, IValidator<LoginRequest> _loginRequestValidator, 
    RefreshHandler _refreshHandler, IValidator<RefreshRequest> _refreshRequestValidator, ILogger<AuthController> logger
    ) : ControllerBase
{
    [HttpPost, Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validatorResult = await _loginRequestValidator.ValidateAsync(request);
        if (!validatorResult.IsValid)
        {
            return Problem(detail: "Invalide input", instance: null, StatusCodes.Status400BadRequest, title: "Bad Request",
                    extensions: new Dictionary<string, object?>{
                    { "erros", validatorResult.Errors.Select(n => n.ErrorMessage).ToArray()}
                    });
        }
        try
        {
            return Ok(await _loginHandler.HandleAsync(request));
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, instance: null, 400, title: "Login Error", type: "Login Error");
        }
    }
}

```

Note: I can also create new instances when I use handlers. In that way, I will not need to register handlers using DI.

I also kept some special handlers to reduce code duplication. For example, the logic to save a image on the server can be share across the application. There could be profile image, chat image, thumbnails...
Here is a generic method for upload images.
```csharp
namespace WebAPI.Features.Images.Command
{
    public record ImageFile(IFormFile File, string? FileDescription);
    public abstract record ImageUploadRequest(ImageFile[] Images);
    public class UploadImageHandler<T>(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment) : Handler(context) where T : Image
    {
        public virtual async Task<T> HandleAsync(T image)
        {
            var trustedFileNameForDisplay = WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(image.File.FileName));
            if (!string.IsNullOrEmpty(image.FileName))
            {
                trustedFileNameForDisplay = WebUtility.HtmlEncode(image.FileName);
            }
            image.FileName = trustedFileNameForDisplay;
            image.FilePath = $"";
            // create the image in the data base first to get the id.
            await _context.AddAsync(image);
            await _context.SaveChangesAsync();

            var request = httpContextAccessor.HttpContext?.Request;
            image.FilePath = $"{request?.Scheme}://{request?.Host}{request?.PathBase}/Images/{image.Id}{image.FileExtension}";
            // create the file path using the generated id to avoid duplicate names.
            await _context.SaveChangesAsync();
            var localFilePath = Path.Combine(webHostEnvironment.ContentRootPath, "Images", $"{image.Id}{image.FileExtension}");
            using (var fileStream = new FileStream(localFilePath, FileMode.Create))
            {
                await image.File.CopyToAsync(fileStream);
            }
            return image;
        }
    }
}
```

A user profile image will just one extra feild, UserProfileID. Upload a profile image will simple be:
```csharp
namespace WebAPI.Features.UserProfiles.Command
{
    public record ProfileImageUploadRequest(ImageFile[] Images, string UserProfileId) : ImageUploadRequest(Images);
    public class UploadProfileImageHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        : UploadImageHandler<ProfileImage>(context, httpContextAccessor, webHostEnvironment)
    {
        public override async Task<ProfileImage> HandleAsync(ProfileImage image)
        {
            var newImage = await base.HandleAsync(image);
            newImage.UserProfileId = image.UserProfileId;
            await _context.SaveChangesAsync();
            return newImage;
        }
    }
}
```