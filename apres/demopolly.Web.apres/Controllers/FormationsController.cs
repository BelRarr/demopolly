using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using demopolly.Web.Models;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.Timeout;

namespace demopolly.Web.Controllers
{
    public class FormationsController : Controller
    {
        private readonly FormationsContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public FormationsController(FormationsContext context, IConfiguration config)
        {
            _httpClient = new HttpClient();
            _configuration = config;
            _context = context;
        }


        // GET: Formations/Create
        public async Task<IActionResult> Create()
        {
            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                ViewBag.Formateurs = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());
            }

            return View();
        }

        // GET: Formations/TimeoutPolicy
        public async Task<IActionResult> TimeoutPolicy()
        {
            // lève un TimeoutRejectedException après 3 secondes.
            var timeoutPolicy = Policy.TimeoutAsync(3);

            var formateurs = new List<string>();
            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs/123";

            var response = timeoutPolicy.ExecuteAsync(async token => await _httpClient.GetAsync(url, token), System.Threading.CancellationToken.None);
            
            if (response.Result.IsSuccessStatusCode)
            {
                formateurs.Add(await response.Result.Content.ReadAsStringAsync());
            }

            ViewBag.Formateurs = formateurs;
            return View("Create");
        }


        // GET: Formations/RetryPolicy
        public async Task<IActionResult> RetryPolicy()
        {
            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                    .RetryAsync(3);

            var formateurs = new List<string>();
            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs/404";

            var response = await retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));

            if (response.IsSuccessStatusCode)
            {
                formateurs.Add(await response.Content.ReadAsStringAsync());
            }

            ViewBag.Formateurs = formateurs;
            return View("Create");
        }


        // GET: Formations/WaitAndRetryPolicy
        public async Task<IActionResult> WaitAndRetryPolicy()
        {
            var waitAndRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                    .Or<System.Net.Sockets.SocketException>()
                                    .WaitAndRetryAsync(3, essai => TimeSpan.FromSeconds(Math.Pow(2, essai)));

            var formateurs = new List<string>();
            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs/404";

            var response = await waitAndRetryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));

            if (response.IsSuccessStatusCode)
            {
                formateurs.Add(await response.Content.ReadAsStringAsync());
            }

            ViewBag.Formateurs = formateurs;
            return View("Create");
        }


        // GET: Formations/FallbackPolicy
        public async Task<IActionResult> FallbackPolicy()
        {
            var formateurs = new List<string>();

            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                   .RetryAsync(3);

            var fallbackPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                       .FallbackAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                                       {
                                           Content = new StringContent("Scott Hanselman")
                                       });


            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs/500";

            var response = await fallbackPolicy.ExecuteAsync(() => retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url)));

            if (response.IsSuccessStatusCode)
            {
                formateurs.Add(await response.Content.ReadAsStringAsync());
            }

            ViewBag.Formateurs = formateurs;
            return View("Create");
        }


        // GET: Formations/RetryPolicyWithDelegate
        public async Task<IActionResult> RetryPolicyWithDelegate()
        {
            var retryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                    .RetryAsync(3, onRetry: (httpResponseMessage, retryCount) => 
                                    {
                                        if (httpResponseMessage.Result.StatusCode == System.Net.HttpStatusCode.NotFound)
                                        {
                                            // logguer l'erreur
                                        }
                                        else if(httpResponseMessage.Result.StatusCode == System.Net.HttpStatusCode.Forbidden)
                                        {
                                            // rediriger vers la page d'accueil
                                        }
                                    });

            var formateurs = new List<string>();
            var baseUrl = _configuration["webserviceBaseUrl"];
            var url = $"{baseUrl}/api/formateurs/404";

            var response = await retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));

            if (response.IsSuccessStatusCode)
            {
                formateurs.Add(await response.Content.ReadAsStringAsync());
            }

            ViewBag.Formateurs = formateurs;
            return View("Create");
        }

        #region Le reste

        // GET: Formations
        public async Task<IActionResult> Index()
        {
            return View(await _context.Formations.ToListAsync());
        }

        // GET: Formations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formation = await _context.Formations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (formation == null)
            {
                return NotFound();
            }

            return View(formation);
        }

        // POST: Formations/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Presentateur,Code,Description,PublicCible,DateHeureDebut,DureeEnHeures,NombrePlacesDisponibles,EstActive")] Formation formation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(formation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(formation);
        }

        // GET: Formations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formation = await _context.Formations.FindAsync(id);
            if (formation == null)
            {
                return NotFound();
            }
            return View(formation);
        }

        // POST: Formations/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Presentateur,Code,Description,PublicCible,DateHeureDebut,DureeEnHeures,NombrePlacesDisponibles,EstActive")] Formation formation)
        {
            if (id != formation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(formation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormationExists(formation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(formation);
        }

        // GET: Formations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formation = await _context.Formations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (formation == null)
            {
                return NotFound();
            }

            return View(formation);
        }

        // POST: Formations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            _context.Formations.Remove(formation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FormationExists(int id)
        {
            return _context.Formations.Any(e => e.Id == id);
        }

        #endregion
    }
}
