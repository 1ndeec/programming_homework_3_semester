// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Pages.Runs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebDebug.Data;
using WebDebug.Domain;

/// <summary>
/// PageModel for the /Runs/Details page.
/// Loads a single test run and its associated test case results by run id.
/// </summary>
public class DetailsModel : PageModel
{
    /// <summary>
    /// EF Core database context used to query TestRun and TestCaseResult entities.
    /// </summary>
    private readonly AppDbContext db;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="db">Application database context.</param>
    public DetailsModel(AppDbContext db) => this.db = db;

    /// <summary>
    /// Gets or sets the requested test run to display. Null when the run is not found.
    /// </summary>
    public TestRun? Run { get; set; } = null!;

    /// <summary>
    /// Gets or sets test case results belonging to the requested run, ordered for display.
    /// </summary>
    public List<TestCaseResult> Results { get; set; } = new();

    /// <summary>
    /// Handles GET requests for the page.
    /// Loads the run by id; returns 404 if missing; otherwise loads its results and renders the page.
    /// </summary>
    /// <param name="id">Identifier of the test run to display.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The page result or a 404 Not Found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        this.Run = await this.db.TestRuns.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (this.Run is null)
        {
            return this.NotFound();
        }

        this.Results = await this.db.TestCaseResults
            .Where(r => r.RunId == id)
            .OrderBy(r => r.FullName)
            .ToListAsync(ct);

        return this.Page();
    }
}