// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Pages.Runs;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebDebug.Data;
using WebDebug.Domain;

/// <summary>
/// PageModel for the /Runs index page.
/// Loads and exposes the list of saved test runs for display in the Razor Page.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>
    /// EF Core database context used to query persisted TestRun entities.
    /// </summary>
    private readonly AppDbContext db;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="db">Application database context.</param>
    public IndexModel(AppDbContext db) => this.db = db;

    /// <summary>
    /// Gets or sets the list of test runs to render on the page.
    /// </summary>
    public List<TestRun> Runs { get; set; } = new();

    /// <summary>
    /// Handles GET requests for the page.
    /// Queries all test runs ordered by newest first.
    /// </summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task OnGetAsync(CancellationToken ct)
    {
        this.Runs = await this.db.TestRuns
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }
}
