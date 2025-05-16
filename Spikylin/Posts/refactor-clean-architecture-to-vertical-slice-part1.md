---
title : Resturcture Clean Architecture to Vertical Slice Arhcitecture Part 1
description:  The part 1 mainly contains the analysis of an old project in Clean Architecture.
date: '2025-01-6'
tags: 
    - ASP.NET
    - Architecture
    - Refactoring
published: true
featured: true
---

This is a template that I built a few years ago. It was first inspired by the techonology I use at work. My goal was to build a fullstack template that would scale and was easy to maintain. The backend is a APS.NET project and the frontend is a Angular SPA. The two frameworks are very similar in design language. They both have well developed and maintained first party tools provided by Microsoft and Google. The tools like Entity Framework and angular-cli are unopinionated the best in their designed use cases. Also, there are many third party components available, like DevExpress and DevExtream. 

Anyway, there are three main reasons to refactor my project. 
- Excessive abstraction makes the project harder to maintain and understand.
- Scattered business logics and DTOs.
- Scattered files across multiple projects and folders.

The old project followed clean architecture and repository design parttern, as you can see from the picture below.
![clean architecture](/post_images/refactor-clean-architecture-to-vertical-slice/clean-architecture.png "Clean Architecture")
My entities are all in the Db project. The business logic lives in the repositories folder. And the controllers are in the controllers folder. The reason that I did not put them in a seperate project was my project was not particularly big, but it is common practice to put them in sperated projects. Under the Repositories folder, I created a generic repository that abstract away Entity Framework that I use. The idea is to reduce code repitition. There are some pros and cons. This approach allows me to switch out Entity Framework if I want to, but the actually layer of abstraction added complexity. Later on, I also learned that repository pattern is redundant if I decide to use Entity Framework. The framework itself is an implementation of repository parttern. 
![repository](/post_images/refactor-clean-architecture-to-vertical-slice/repository.png "Repository")

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
The generic repository pattern implements all the CRUD functions except Update because it is always different. Maybe there is a way to also make it generic.

For a simple CRUD controller, I just need to inject the generic repository into the controller:
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
What if there are special cases? For example, when I try to save an image, I will need to save the file to the server beside create a record in the database. See example below.
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
In this case, I overwrite the existing method. The example above provides specific business logic for saving an image. A bonus, if there are different kinds of image, they can all share part of saving image logic. For example, I have a ProfileImage entity that needs to set the user profile Id after saving an image.
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
It can simply just call the base method to save an image.

However, all the benefits end here basically. 
Because of the nature of clean architecture, files are grouped by technical terms and functions. All the repositories live in a folder and all controllers live in a folder. It is hard to tell what a file does by just looking at the file name. For example, it will be hard to tell the different between the ImageRepository and ProfileImageRepository without going through the code. And, I also do not like the fact the Update method cannot be generic. (Maybe there is way?) It means that I have to create a reporsitory just for the update or do something like:
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
It leaks the business logic to the controller which is not good.

Anothe problem is that DTOs are everywhere. They are in the controllers and repositories, and it is confusing when there are look-alike DTOs. I also find it hard to name them properly for their specific use cases. For the chatting room alone, I already have four DTOs:
![DTO](/post_images/refactor-clean-architecture-to-vertical-slice/DTO.png)

The repository pattern has some benefits but I do not see it to scale well, besides it is not necessary if I use Entity Framework. There are too much abstraction and too many layers. The "Go To Implementation" feature does not work sometimes in Visual Studio. I can see when the project gets bigger it will be hard to maintain with different projects, file locations and abstractions.
