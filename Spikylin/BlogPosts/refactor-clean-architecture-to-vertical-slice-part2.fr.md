---
title: Restructurer la Clean Architecture vers une architecture Vertical Slice Partie 2
description: Refactoriser un projet Clean Architecture vers un projet Vertical Slice Architecture, en remplaçant le repository pattern par du CQRS.
date: '2025-02-02'
tags:
  - ASP.NET
  - Architecture
  - Refactoring
published: true
featured: false
---

Add-Migration SomeMigration -OutputDir Data\Migrations

J'ai décidé de changer l'architecture de Clean Architecture vers CQRS. Il y a plusieurs raisons à cela :
1. Réduire l'abstraction.
2. Augmenter la lisibilité.
3. Adopter une structure de fichiers orientée fonctionnalités.
4. Réduire les conflits de fusion.

Dans mon ancienne clean architecture, toute la logique métier résidait dans le fichier AuthRepository.cs. Maintenant, cette logique est séparée dans différentes commandes :
![alt text](/images/post_images/refactor-clean-architecture-to-vertical-slice/CQRS-file-structure.png)
Cependant, je n'ai pas placé les méthodes de contrôleur dans les handlers, car j'utilise encore des méthodes de contrôleur traditionnelles. Je trouve qu'il est préférable de regrouper tous les endpoints dans une même classe afin de bénéficier des fonctionnalités de groupement et de nommage du framework. Un inconvénient est que si j'ai beaucoup plus d'endpoints pour une seule fonctionnalité, la classe du contrôleur deviendra très volumineuse. Une solution potentielle serait d'utiliser des classes partielles. Une autre option est Minimal API, et c'est probablement un meilleur choix si vous voulez placer les endpoints dans les mêmes fichiers que les handlers.

J'ai aussi ajouté Fluent Validator. Cela rend la validation plus propre. Je l'utilise principalement pour valider les modèles de données, pas la logique métier.

Voici le handler de connexion.
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
Voici la méthode de contrôleur pour la connexion.
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

Note : je peux aussi créer de nouvelles instances au moment d'utiliser les handlers. Dans ce cas, je n'aurais pas besoin d'enregistrer les handlers dans le conteneur DI.

J'ai également conservé certains handlers spécifiques pour réduire la duplication de code. Par exemple, la logique de sauvegarde d'une image sur le serveur peut être partagée dans toute l'application. Il peut s'agir d'une image de profil, d'une image de chat, de miniatures, etc.
Voici une méthode générique pour téléverser des images.
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
Une image de profil utilisateur ajoute simplement un champ supplémentaire, `UserProfileID`. Téléverser une image de profil devient alors :
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

```