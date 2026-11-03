using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record CustomerRequest(string Name, string Address, string? Phone);
public record CustomerResponse(Guid Id, string Name, string Address, string? Phone);

[ApiController]
[Route("api/customers")]
[Authorize(Roles = Roles.Supervisor)]
public class CustomersController(FleetTrackDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> List()
    {
        var customers = await db.Customers
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Address, c.Phone))
            .ToListAsync();

        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CustomerRequest request)
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = request.Name, Address = request.Address, Phone = request.Phone };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var response = new CustomerResponse(customer.Id, customer.Name, customer.Address, customer.Phone);
        return CreatedAtAction(nameof(List), response);
    }
}
