using System.Security.Claims;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain;
using FleetTrack.Domain.Entities;
using FleetTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetTrack.Api.Controllers;

public record DeliveryStatusResponse(Guid Id, string Status);

[ApiController]
[Route("api/deliveries")]
[Authorize(Roles = Roles.Driver)]
public class DeliveriesController(FleetTrackDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private static readonly HashSet<string> AllowedEvidenceTypes = ["image/jpeg", "image/png"];
    private const long MaxEvidenceBytes = 5 * 1024 * 1024;

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<DeliveryStatusResponse>> Start(Guid id)
    {
        var delivery = await LoadOwnedDeliveryAsync(id);
        if (delivery is null)
        {
            return Forbid();
        }
        if (delivery is { Status: DeliveryStatus.Pending })
        {
            delivery.Status = DeliveryStatus.InProgress;
            delivery.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return Ok(new DeliveryStatusResponse(delivery.Id, delivery.Status.ToString()));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<DeliveryStatusResponse>> Complete(Guid id)
    {
        var delivery = await LoadOwnedDeliveryAsync(id);
        if (delivery is null)
        {
            return Forbid();
        }

        // Idempotente: una segunda llamada (reenvio por reconexion) no vuelve a procesar la entrega.
        if (delivery.Status != DeliveryStatus.Completed)
        {
            delivery.Status = DeliveryStatus.Completed;
            delivery.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return Ok(new DeliveryStatusResponse(delivery.Id, delivery.Status.ToString()));
    }

    [HttpPost("{id:guid}/evidence")]
    [RequestSizeLimit(MaxEvidenceBytes)]
    public async Task<IActionResult> UploadEvidence(Guid id, IFormFile file)
    {
        var delivery = await LoadOwnedDeliveryAsync(id);
        if (delivery is null)
        {
            return Forbid();
        }

        if (file.Length == 0 || file.Length > MaxEvidenceBytes || !AllowedEvidenceTypes.Contains(file.ContentType))
        {
            return BadRequest("Evidencia invalida: solo JPEG/PNG hasta 5 MB.");
        }

        var extension = file.ContentType == "image/png" ? ".png" : ".jpg";
        var fileName = $"{Guid.NewGuid()}{extension}";
        var evidenceDir = Path.Combine(env.ContentRootPath, "evidence");
        Directory.CreateDirectory(evidenceDir);

        await using (var stream = System.IO.File.Create(Path.Combine(evidenceDir, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        db.DeliveryEvidences.Add(new DeliveryEvidence
        {
            Id = Guid.NewGuid(),
            DeliveryId = delivery.Id,
            FileName = fileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length
        });
        await db.SaveChangesAsync();

        return Ok();
    }

    private async Task<Delivery?> LoadOwnedDeliveryAsync(Guid deliveryId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        return await db.Deliveries
            .Include(d => d.Route)
            .Where(d => d.Id == deliveryId && d.Route!.Driver!.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
