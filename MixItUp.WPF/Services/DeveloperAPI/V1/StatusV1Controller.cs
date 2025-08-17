using System;
using System.Reflection;
using System.Web.Http;

namespace MixItUp.WPF.Services.DeveloperAPI.V1
{
    [RoutePrefix("api/status")]
    [Obsolete("This v1 API controller is deprecated. Please use the V2 API.")]
    public class StatusV1Controller : ApiController
    {
        [Route("version")]
        [HttpGet]
        public IHttpActionResult GetVersion()
        {
            return Ok(Assembly.GetEntryAssembly().GetName().Version.ToString());
        }
    }
}
