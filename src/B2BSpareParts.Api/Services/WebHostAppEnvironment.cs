using B2BSpareParts.Application.Contracts;

namespace B2BSpareParts.Api.Services;

public class WebHostAppEnvironment : IAppEnvironment
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public WebHostAppEnvironment(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public string ContentRootPath => _webHostEnvironment.ContentRootPath;
}
