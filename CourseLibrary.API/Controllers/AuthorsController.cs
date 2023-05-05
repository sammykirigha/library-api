
using AutoMapper;
using CourseLibrary.API.AddConstraints;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly ITypeHasPropertiesService _typeHasPropertiesService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IPropertyMappingService propertyMappingService,
        ITypeHasPropertiesService typeHasProperties,
        ProblemDetailsFactory problemDetailsFactory
        )
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ?? 
            throw new ArgumentNullException(nameof(propertyMappingService));
        _typeHasPropertiesService = typeHasProperties ?? 
            throw new ArgumentNullException(nameof(typeHasProperties));
        _problemDetailsFactory = problemDetailsFactory ??
            throw new ArgumentNullException(nameof(problemDetailsFactory));
    }

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    public async Task<IActionResult> GetAuthors(
      [FromQuery] AuthorsResourceParameters authorsResourceParameters
        )
    { 
        //throw new Exception("Test exception)
        if(!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
        {
            return BadRequest();
        }

        if(!_typeHasPropertiesService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                 statusCode: 400,
                 detail: $"Not all requested data shaping fields exist on " + $"the source: {authorsResourceParameters.Fields}")
                );
        }
       
        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameters); 

 /*       var previousPageLink = authorsFromRepo.HasPrevious ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage) : null;
        var nextPageLink = authorsFromRepo.HasNext ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage) : null;*/

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages
           /* previousPageLink = previousPageLink,
            nextPageLink = nextPageLink*/
        };

        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        //create links
        var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);
        var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo).ShapeData(authorsResourceParameters.Fields);
        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object?>;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links = links
        };
        // return them
        return Ok(linkedCollectionResource);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>();
        links.Add(
            new(CreateAuthorsResourceUri(authorsResourceParameters, 
            ResourceUriType.Current), 
            "self", 
            "GET"));
        if(hasNext)
        {
            links.Add(
            new(CreateAuthorsResourceUri(authorsResourceParameters,
            ResourceUriType.NextPage),
            "nextPage",
            "GET"));
        }

        if(hasPrevious)
        {
            links.Add(
            new(CreateAuthorsResourceUri(authorsResourceParameters,
            ResourceUriType.PreviousPage),
            "previousPage",
            "GET"));
        }

        return links;
    }

    private string? CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters,
        ResourceUriType type)
    {
        switch(type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors", 
                    new
                    {
                      fields = authorsResourceParameters.Fields,
                      orderBy = authorsResourceParameters.OrderBy,
                      pageNumber = authorsResourceParameters.PageNumber - 1,
                      pageSize = authorsResourceParameters.PageSize,
                      mainCategory = authorsResourceParameters.MainCategory,
                      searchQuery = authorsResourceParameters.SearchQuery,
                    });

            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                     new
                     {
                         fields = authorsResourceParameters.Fields,
                         orderBy = authorsResourceParameters.OrderBy,
                         pageNumber = authorsResourceParameters.PageNumber + 1,
                         pageSize = authorsResourceParameters.PageSize,
                         mainCategory = authorsResourceParameters.MainCategory,
                         searchQuery = authorsResourceParameters.SearchQuery,
                     });
            case ResourceUriType.Current:
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                        orderBy = authorsResourceParameters.OrderBy,
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery,
                    });
        }
    }

    [Produces("application/json",
        "application/sam.dotnet.author.full+json", 
        "application/sam.dotnet.author.full.hateoas+json", 
        "application/sam.dotnet.hateoas+json", 
        "application/sam.dotnet.friendly+json", 
        "application/sam.dotnet.friendly.hateoas+json")]
    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<IActionResult> GetAuthor(Guid authorId, string? fields, [FromHeader(Name ="Accept")] string? mediaType)
    {

        if(!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                statusCode: 400,
                detail: $"Accept header media type value is not a valid media type."
                )
                );
        }
        //text for exception
        if (!_typeHasPropertiesService.TypeHasProperties<AuthorDto>(fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                 statusCode: 400,
                 detail: $"Not all requested data shaping fields exist on " + $"the source: {fields}")
                );
        }
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);
        IEnumerable<LinkDto> links = new List<LinkDto>();

        if (includeLinks)
        {
            links = CreateLinksForAuthor(authorId, fields);
        }

        var primaryMediaTypes = includeLinks ? 
            parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8) 
            : parsedMediaType.SubTypeWithoutSuffix;

        //full author

        if(primaryMediaTypes == "sam.dotnet.author.full")
        {
            var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object>;

            if(includeLinks)
            {
                fullResourceToReturn.Add("links", links);
            }

            return Ok(fullResourceToReturn);

        }

        //friendly author
        var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo).ShapeData(fields) as IDictionary<string, object>;

        if(includeLinks)
        {
            friendlyResourceToReturn.Add("links", links);
        }

        return Ok(friendlyResourceToReturn);

       
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
    {
        var links = new List<LinkDto>();

        if(string.IsNullOrWhiteSpace(fields))
        {
            links.Add(new(Url.Link("GetAuthor", new { authorId }), "self", "GET"));
        }else
        {
            links.Add(
                new(Url.Link("GetAuthor", new { authorId, fields }), "self", "GET"));
        }

        links.Add(new(Url.Link("CreateCourseForAuthor", new { authorId }), "create_course_for_author", "POST"));


        links.Add(new(Url.Link("GetCoursesForAuthor", new { authorId }), "courses", "GET"));

        return links;
    }

    [HttpPost(Name = "CreateAuthor")]
    [RequestHeaderMatchesMediaType("Content-Type", "application/json", "application/sam.dotnet.authorforcreation+json")]
    [Consumes("application/json", "application/sam.dotnet.authorforcreation+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorToReturn).ShapeData(null) as IDictionary<string, object?>;

        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }


    [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
    [RequestHeaderMatchesMediaType("Content-Type", "application/sam.dotnet.authorforcreationwithdateofdeath+json")]
    [Consumes("application/sam.dotnet.authorforcreationwithdateofdeath+json")]
    public async Task<ActionResult<AuthorDto>> CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto author)
    {
        var authorEntity = _mapper.Map<Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorToReturn).ShapeData(null) as IDictionary<string, object?>;

        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);
    }

    [HttpOptions()]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET, HEAD, POST, OPTIONS");
        return Ok();
    }


}

