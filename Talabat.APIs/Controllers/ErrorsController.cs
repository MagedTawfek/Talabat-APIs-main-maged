using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Talabat.APIs.Errors;

namespace Talabat.APIs.Controllers
{
    [Route("errors/{code}")]
    [ApiController]
    [AllowAnonymous]
    public class ErrorsController : ControllerBase
    {
        [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public IActionResult Error(int code)
        {
            if (code is < 400 or > 599)
            {
                return BadRequest(new ApiResponse(400, "Error status code must be between 400 and 599."));
            }

            return new ObjectResult(new ApiResponse(code))
            {
                StatusCode = code
            };
        }
    }
}