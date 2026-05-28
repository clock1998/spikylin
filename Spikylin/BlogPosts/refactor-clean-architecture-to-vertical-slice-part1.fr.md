---
title: Restructurer la Clean Architecture vers une architecture Vertical Slice Partie 1
description: La partie 1 contient principalement l'analyse d'un ancien projet en Clean Architecture.
date: '2025-01-6'
tags:
  - ASP.NET
  - Architecture
  - Refactoring
published: true
featured: true
---

Il s'agit d'un template que j'ai construit il y a quelques années. Il a d'abord été inspiré par les technologies que j'utilise au travail. Mon objectif était de construire un template full stack qui puisse évoluer et rester facile à maintenir. Le backend est un projet ASP.NET et le frontend est une SPA Angular. Les deux frameworks sont assez proches dans leur philosophie de conception. Ils disposent tous deux d'outils first party bien développés et bien maintenus, fournis par Microsoft et Google. Des outils comme Entity Framework et angular-cli sont, selon moi, parmi les meilleurs dans leur cas d'usage. Il existe aussi de nombreux composants tiers, comme DevExpress et DevExtreme.

Bref, il y a trois raisons principales de refactorer mon projet.
- Une abstraction excessive rend le projet plus difficile à maintenir et à comprendre.
- La logique métier et les DTO sont dispersés.
- Les fichiers sont éparpillés dans plusieurs projets et dossiers.

L'ancien projet suivait la clean architecture et le repository pattern, comme on peut le voir sur l'image ci-dessous.
![clean architecture](/images/post_images/refactor-clean-architecture-to-vertical-slice/clean-architecture.png "Clean Architecture")
Mes entités sont toutes dans le projet Db. La logique métier vit dans le dossier des repositories. Et les contrôleurs sont dans le dossier controllers. La raison pour laquelle je ne les ai pas mis dans des projets séparés est que mon projet n'était pas particulièrement gros, mais il est courant de les séparer. Sous le dossier Repositories, j'ai créé un repository générique qui abstrait Entity Framework. L'idée était de réduire la répétition de code. Cette approche a des avantages et des inconvénients. Elle me permet de remplacer Entity Framework si je le souhaite, mais cette couche d'abstraction ajoute en réalité de la complexité. Plus tard, j'ai aussi appris que le repository pattern est redondant si je décide d'utiliser Entity Framework. Le framework lui-même est déjà une implémentation du repository pattern.
![repository](/images/post_images/refactor-clean-architecture-to-vertical-slice/repository.png "Repository")

```csharp
public interface IRepository<T> where T : class
{
    public IQueryable<T> GetAll(QueryStringParameters queryParameters);

    public Task<T> GetAsync(Guid id);

    public Task<T> CreateAsync(T model);

    public void UpdateAsync(Guid id, T model);
    public Task SaveChangesAsync();

    public Task<T> Delete(Guid id);
}

public class Repository<T> : IRepository<T> where T : class
{
    public readonly AppDbContext _dbContext;
    private DbSet<T> _dbSet;
    public Repository(AppDbContext context)
    {
        _dbContext = context;
        _dbSet = _dbContext.Set<T>();
    }
    public virtual IQueryable<T> GetAll(QueryStringParameters queryParameters)
    {
        return _dbSet;
        //return await Task.FromResult(context.Set<T>().AsQueryable().OrderBy(n => n.Id).Skip((queryParameters.PageNumber-1) * queryParameters.PageSize).Take(queryParameters.PageSize));
    }
    public virtual async Task<T> GetAsync(Guid id)
    {
        var result = await _dbSet.FindAsync(id);
        if(result == null) throw new InvalidOperationException($"A {typeof(T)} with ID {id} was not found.");
        return result;
    }

    public virtual async Task<T> CreateAsync(T model)
    {
        await _dbSet.AddAsync(model);
        return model;
    }

    public virtual void UpdateAsync(Guid id, T model)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<T> Delete(Guid id)
    {
        var model = await GetAsync(id);
        if (model == null) throw new InvalidOperationException($"A {typeof(T)} with ID {id} was not found.");
        _dbSet.Remove(model);
        await _dbContext.SaveChangesAsync();
        return model;
    }
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
```
Le repository générique implémente toutes les opérations CRUD sauf Update, car cette partie est toujours différente. Il existe peut-être un moyen de le rendre lui aussi générique.

Pour un contrôleur CRUD simple, j'ai seulement besoin d'injecter le repository générique dans le contrôleur :
```csharp
public class CourseController : ControllerBase
{
    private readonly AppDbContext context;
    private readonly IRepository<Course> courseRepository;

    public CourseController(AppDbContext context, IRepository<Course> courseRepository)
    {
        this.context = context;
        this.courseRepository = courseRepository;
    }
    class CourseViewModel
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Semester { get; set; }

        public Guid SemesterId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
    }
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CourseParameters queryParameters)
    {
        var query = courseRepository.GetAll(queryParameters).Select(n => new CourseViewModel
        {
            Id = n.Id,
            Name = n.Name,
            Semester = n.Semester.Name,
            SemesterId = n.SemesterId,
            Updated = n.Updated,
            Created = n.Created,
        });

        var data = await PagedList<CourseViewModel>.ToPagedListAsync(
            query,
            queryParameters.PageNumber,
            queryParameters.PageSize);
        Response.Headers.Append("X-Pagination", data.GeneratePagedMeta());
        return Ok(data);
    }

    [HttpGet]
    [Route("{id:Guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        try
        {
            var model = await courseRepository.GetAsync(id);
            return Ok(new CourseDTO { Name = model.Name, Section = model.Section, SemesterId = model.SemesterId });
        }
        catch (Exception ex) {
            return NotFound(ex);
        }            
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CourseDTO courseDTO)
    {
        Course model = new Course
        {
            Name = courseDTO.Name,
            Section = courseDTO.Section,
            SemesterId = courseDTO.SemesterId,
        };
        await courseRepository.CreateAsync(model);
        return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
    }

    [HttpPut]
    [Route("{id:Guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CourseDTO courseDTO)
    {
        var course = await courseRepository.GetAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        course.Section = courseDTO.Section;
        course.Name = courseDTO.Name;
        course.SemesterId = courseDTO.SemesterId;
        await courseRepository.SaveChangesAsync();
        return Ok(course);
    }

    [HttpDelete]
    [Route("{id:Guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var model = await courseRepository.Delete(id);
            return Ok(model);
        }
        catch (Exception ex) {
            return NotFound(ex);
        }
    }
}
```
Que faire s'il existe des cas particuliers ? Par exemple, lorsque je veux enregistrer une image, je dois sauvegarder le fichier sur le serveur en plus de créer un enregistrement en base de données. Voir l'exemple ci-dessous.
```csharp
public class ImageRepository<T> : Repository<T>, IImageRepository<T> where T : Image
    {
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ImageRepository(AppDbContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor) : base(context)
        {
            this.webHostEnvironment = webHostEnvironment;
            this.httpContextAccessor = httpContextAccessor;
        }
        public override async Task<T> CreateAsync(T image)
        {
            var trustedFileNameForDisplay = WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(image.File.FileName));
            if (!string.IsNullOrEmpty(image.FileName))
            {
                trustedFileNameForDisplay = WebUtility.HtmlEncode(image.FileName);
            }
            image.FileName = trustedFileNameForDisplay;
            image.FilePath = $"";
            // create the image in the data base first to get the id.
            await _dbContext.AddAsync(image);
            await _dbContext.SaveChangesAsync();

            var request = httpContextAccessor.HttpContext?.Request;
            image.FilePath = $"{request?.Scheme}://{request?.Host}{request?.PathBase}/Images/{image.Id}{image.FileExtension}";
            // create the file path using the generated id to avoid duplicate names.
            await _dbContext.SaveChangesAsync();
            var localFilePath = Path.Combine(webHostEnvironment.ContentRootPath, "Images", $"{image.Id}{image.FileExtension}");
            using (var fileStream = new FileStream(localFilePath, FileMode.Create))
            {
                await image.File.CopyToAsync(fileStream);
            }
            return image;
        }

        public override async Task<T> Delete(Guid id)
        {
            var model = await base.Delete(id);
            var localFilePath = Path.Combine(webHostEnvironment.ContentRootPath, "Images", $"{model.Id}{model.FileExtension}");
            try
            {
                // Check if file exists with its full path
                if (File.Exists(localFilePath))
                {
                    // If file found, delete it
                    File.Delete(localFilePath);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException("File path does not exist.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return model;
        }
    }
```
Dans ce cas, j'écrase la méthode existante. L'exemple ci-dessus fournit une logique métier spécifique pour sauvegarder une image. En bonus, s'il existe différents types d'images, ils peuvent tous partager une partie de cette logique de sauvegarde. Par exemple, j'ai une entité `ProfileImage` qui doit définir l'identifiant du profil utilisateur après la sauvegarde d'une image.
```csharp
public class ProfileImageRepository : ImageRepository<ProfileImage>
{
    public ProfileImageRepository(AppDbContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor) : base(context, webHostEnvironment, httpContextAccessor)
    {
    }
    public override async Task<ProfileImage> CreateAsync(ProfileImage image)
    {
        var newImage = await base.CreateAsync(image);
        newImage.UserProfileId = image.UserProfileId;
        await _dbContext.SaveChangesAsync();
        return newImage;
    }
}
```
Il suffit donc d'appeler la méthode de base pour enregistrer l'image.

Cependant, pratiquement tous les bénéfices s'arrêtent là.
Parce que, par nature, la clean architecture regroupe les fichiers par termes techniques et par fonctions. Tous les repositories vivent dans un dossier et tous les contrôleurs dans un autre. Il est difficile de savoir ce que fait un fichier juste en regardant son nom. Par exemple, il est difficile de voir la différence entre `ImageRepository` et `ProfileImageRepository` sans parcourir le code. Et je n'aime pas non plus le fait que la méthode `Update` ne puisse pas être générique. (Il y a peut-être une solution ?) Cela signifie que je dois créer un repository juste pour l'update ou faire quelque chose comme ceci :
```csharp
[HttpPut]
[Route("{id:Guid}")]
public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SemesterDTO semesterDTO)
{
    var updatedSemester = await repository.GetAsync(id);
    if (updatedSemester == null)
    {
        return NotFound();
    }
    updatedSemester.Name = semesterDTO.Name;
    updatedSemester.Year = semesterDTO.Year;
    await repository.SaveChangesAsync();
    return Ok(updatedSemester);
}
```
Cela fait fuir la logique métier vers le contrôleur, ce qui n'est pas une bonne chose.

Un autre problème est que les DTO sont partout. Ils sont dans les contrôleurs et dans les repositories, et cela devient confus lorsqu'il existe plusieurs DTO qui se ressemblent. Je trouve aussi difficile de les nommer correctement selon leur cas d'usage précis. Pour la salle de chat à elle seule, j'ai déjà quatre DTO :
![DTO](/images/post_images/refactor-clean-architecture-to-vertical-slice/DTO.png)

Le repository pattern a certains avantages, mais je ne le vois pas bien évoluer dans le temps, surtout qu'il n'est pas nécessaire si j'utilise Entity Framework. Il y a trop d'abstraction et trop de couches. La fonctionnalité "Go To Implementation" ne fonctionne parfois pas dans Visual Studio. Je vois bien que, lorsque le projet grossira, il deviendra difficile à maintenir avec différents projets, emplacements de fichiers et niveaux d'abstraction.

```