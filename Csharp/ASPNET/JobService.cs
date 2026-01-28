using Microsoft.EntityFrameworkCore;
using Joblin.Data;
using Joblin.Models;

public class JobService
{
    private readonly AppDbContext _db;
    public JobService(AppDbContext db) => _db = db;
    public List<Job> jobs = new();

    /// <summary>
    /// Retuns the jobs as a List<Job>
    /// </summary>
    /// <returns>The jobs list when retrieved from the database</returns>
    public async Task<List<Job>> GetJobsAsync()
    {
        return await _db.Jobs.AsNoTracking().ToListAsync();
    }
    
    /// <summary>
    /// Add a job to the database
    /// </summary>
    /// <param name="job">The job to add</param>
    /// <returns>True if the operation succeeded, false if a job with the same link was already in the database.</returns>
    public async Task<bool> SaveJob(Job job)
    {
        try
        {
            var normalizedLink = job.Link.Trim();
            job.Link = normalizedLink;

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();

            jobs.Add(job);
            return true;
        }
        catch (DbUpdateException e)
        {
            Console.WriteLine(e);
            Console.WriteLine($"Doublon ignoré : {job.Link}");
            //stop l'ajout de cet objet avec un lien non unique
            _db.Entry(job).State = EntityState.Detached;
            return false;
        }
    }

    /// <summary>
    /// Update the checkboxes data from the frontend
    /// </summary>
    /// <param name="id">ID of the job</param>
    /// <param name="checkbox">The checkbox field</param>
    /// <param name="value">The value from the HTML checkbox</param>
    /// <returns>True if the Job could be updated, false otherwise</returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> UpdateJob(int id, JobCheckbox checkbox, bool value)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return false;

        switch (checkbox)
        {
            case JobCheckbox.isChecked: job.isCheckmark = value; break;
            case JobCheckbox.isApplied: job.isApplied = value; break;
            case JobCheckbox.isHidden: job.isHidden = value; break;
            default: throw new Exception("Invalid key");
        }
        ;

        await _db.SaveChangesAsync();

        job = jobs.FirstOrDefault(j => j.Id == id);
        if (job is not null)
        {
            switch (checkbox)
            {
                case JobCheckbox.isChecked: job.isCheckmark = value; break;
                case JobCheckbox.isApplied: job.isApplied = value; break;
                case JobCheckbox.isHidden: job.isHidden = value; break;
                default: throw new Exception("Invalid key");
            }
        }
        return true;
    }
}