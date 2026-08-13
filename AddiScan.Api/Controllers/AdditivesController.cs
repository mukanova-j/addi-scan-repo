using AddiScan.Api.Auth;
using AddiScan.Api.Contracts;
using AddiScan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Api.Controllers;

[ApiController]
[Route("api/additives")]
public class AdditivesController(AddiScanDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Lists every additive in the reference dataset with a summary of its safety grade. A
    /// logged-in caller with functional necessity turned off in their settings sees totals
    /// recalculated without that modifier.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AdditiveSummaryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var includeFunctionalNecessity = await GradingPreference.IncludeFunctionalNecessityAsync(User, dbContext, cancellationToken);

        var additives = await dbContext.Additives
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return Ok(additives.Select(a => AdditiveSummaryResponse.FromEntity(a, includeFunctionalNecessity)).ToList());
    }

    /// <summary>
    /// Returns the full detail for a single additive, including the per-criterion grading breakdown.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdditiveDetailResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var includeFunctionalNecessity = await GradingPreference.IncludeFunctionalNecessityAsync(User, dbContext, cancellationToken);

        var additive = await dbContext.Additives.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (additive is null)
        {
            return NotFound();
        }

        return Ok(AdditiveDetailResponse.FromEntity(additive, includeFunctionalNecessity));
    }
}
