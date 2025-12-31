// Copyright (c) Murat Khamatyanov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WebDebug.Data;

using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using WebDebug.Domain;

/// <summary>
/// Application EF Core database context.
/// Defines the entity sets and configures relationships between them.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class using the provided options.
    /// </summary>
    /// <param name="options">EF Core context options (provider, connection string, etc.).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets table-like set of persisted test runs.
    /// </summary>
    public DbSet<TestRun> TestRuns => this.Set<TestRun>();

    /// <summary>
    /// Gets table-like set of persisted test case results belonging to runs.
    /// </summary>
    public DbSet<TestCaseResult> TestCaseResults => this.Set<TestCaseResult>();

    /// <summary>
    /// Configures the EF Core model.
    /// Defines the one-to-many relationship: one <see cref="TestRun"/> has many results identified by <c>RunId</c>.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to configure entity mappings.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestRun>()
            .HasMany(r => r.Results)
            .WithOne()
            .HasForeignKey(r => r.RunId);
    }
}
