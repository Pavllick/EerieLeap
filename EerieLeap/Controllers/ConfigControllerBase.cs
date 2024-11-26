using Microsoft.AspNetCore.Mvc;

namespace EerieLeap.Controllers;

[ApiController]
[Route("api/v1/config")]
public abstract class ConfigControllerBase : ControllerBase {
    protected readonly ILogger Logger;

    protected ConfigControllerBase(ILogger logger) =>
        Logger = logger;
}
