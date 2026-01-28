using Joblin.Data;
using Joblin.Models;
using Joblin.Services;
using Joblin.Helpers;

namespace Joblin.ApiEndpoints;

/// <summary>
/// Job routes management
/// </summary>
public static class JobEndpoints
{

    public static void MapRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/getkeywords", async (KeywordService kwService) =>
        {
            // reception d'une demande de keywords par l'ext chrome
                bool isKWOkay = await kwService.GetKeywords();

                if(!isKWOkay)
                    return Results.Problem("Impossible de récupérer les allow- et denylists");
  
                var data = new
                {
                    message = "Keywords disponibles envoyés",
                    whitelist = kwService.AllowList.ToArray(),
                    blacklist = kwService.DenyList.ToArray()
                };

                return Results.Ok(data);

        }).WithName("GetKeywords");

        app.MapPost("/savejob", async (JobData res, AppDbContext db,  JobService service, ServerStatusService server) =>
        {

            var serverStatusError = RouteHelpers.CheckServerStatus(server);
            if(serverStatusError is not null) return serverStatusError;

            Console.WriteLine("Job data received : " + res.title);
            var jobToSave = new Job
            {
                Title = res.title,
                Company = res.company,
                Link = res.link,
                Location = res.location
            };

            if (await service.SaveJob(jobToSave))
            {
                Console.WriteLine("Added " + res.title + " to the DB");
                return Results.Ok(res.title + " job ajouté a la DB");
            }
            Console.WriteLine("Job already in DB, not added.");
            return Results.Conflict("Job has already been registred");

        }).WithName("SaveJob");

        app.MapPost("/updatejob", async (JobUpdate res, AppDbContext db,  JobService service, ServerStatusService server) =>
        {
            var serverStatusError = RouteHelpers.CheckServerStatus(server);
            if(serverStatusError is not null) return serverStatusError;

            Console.WriteLine("Job data to update : " + res.Id);

            if(await service.UpdateJob(res.Id, res.Key, res.value))
            {
                return Results.Ok("Job updated");
            }
            return Results.NotFound("Job ID was not found");
            
        }).WithName("UpdateJob");
    }
}