using Microsoft.AspNetCore.Mvc;
namespace DefaultNamespace.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController: ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "Hello, World!";
    }
    [HttpGet("{name}")]
    public string GetByName(string name)
    {
        return $"Hello, {name}!";
    }
}