// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebDebug.Data;
using WebDebug.Domain;

/// <summary>
/// PageModel for the upload page.
/// Accepts one or more .dll files, runs MyNUnit on them, and stores the results.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext db;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="db">Application database context.</param>
    public IndexModel(AppDbContext db)
    {
        this.db = db;
    }

    /// <summary>
    /// Gets or sets uploaded files bound from the multipart/form-data request.
    /// </summary>
    [BindProperty]
    public List<IFormFile> Files { get; set; } = new();

    /// <summary>
    /// Handles GET requests and renders the upload form.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Handles POST requests: validates uploads, saves assemblies, runs tests, persists the run and results, and redirects to details.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A page result on validation error, or a redirect to the run details page on success.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (this.Files.Count == 0)
        {
            this.ModelState.AddModelError(string.Empty, "Upload at least one .dll file.");
            return this.Page();
        }

        if (this.Files.Any(f => !f.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
        {
            this.ModelState.AddModelError(string.Empty, "Only .dll files are allowed.");
            return this.Page();
        }

        var runId = Guid.NewGuid();
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "runs", runId.ToString());
        Directory.CreateDirectory(folder);

        foreach (var file in this.Files)
        {
            var safeName = Path.GetFileName(file.FileName);
            var path = Path.Combine(folder, safeName);

            await using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream, ct);
        }

        var runner = new MyNUnit.SimpleNUnit();
        var results = await runner.RunAsync(folder);

        var run = new TestRun
        {
            Id = runId,
            CreatedAt = DateTime.UtcNow,
            Total = results.Count,
            Passed = results.Count(r => r.Status == MyNUnit.TestStatus.Passed),
            Failed = results.Count(r => r.Status == MyNUnit.TestStatus.Failed),
            Skipped = results.Count(r => r.Status == MyNUnit.TestStatus.Skipped),
        };

        this.db.TestRuns.Add(run);

        foreach (var r in results)
        {
            this.db.TestCaseResults.Add(new TestCaseResult
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                FullName = $"{r.Method.DeclaringType?.FullName}.{r.Method.Name}",
                Status = r.Status.ToString(),
                DurationMs = r.Duration.TotalMilliseconds,
                Message = r.Message,
            });
        }

        await this.db.SaveChangesAsync(ct);

        return this.RedirectToPage("/Runs/Details", new { id = runId });
    }
}
