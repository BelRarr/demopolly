using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace demopolly.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormateursController : ControllerBase
    {
        // GET api/formateurs
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "Tidjani Belmansour", "Bruno Sonnino", "Hamida Rebai" };
        }


        // GET api/formateurs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id)
        {
            if (id == 404)
                return NotFound();

            if (id == 500)
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError);

            // pour simuler une action qui prend trop de temps et qu'on veut donc interrompre avec un Timeout Policy
            await Task.Delay(10000);
            return "Bruno Sonnino";
        }        
    }
}
