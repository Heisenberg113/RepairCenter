using System.Data;
using Microsoft.EntityFrameworkCore;
using RepairCenter.Data;
using RepairCenter.DTOs.RepairTickets;
using RepairCenter.Entities;
using RepairCenter.Enums;
using RepairCenter.Contracts;
using RepairCenter.Repositories.Interfaces;
using RepairCenter.Utilities;

namespace RepairCenter.Repositories;

public sealed class RepairIntakeRepository(RepairCenterDbContext context) : IRepairIntakeRepository
{
    public async Task<(RepairTicket? Ticket, RepairIntakeError Error)> CreateAsync(CreateRepairIntakeRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        Customer customer;
        if (request.NewCustomer is not null)
        {
            customer = new Customer
            {
                Name = request.NewCustomer.Name.Trim(),
                Email = Normalize(request.NewCustomer.Email)?.ToLowerInvariant(),
                PhoneNumber = Normalize(request.NewCustomer.PhoneNumber),
                Address = Normalize(request.NewCustomer.Address)
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }
        else
        {
            var existingCustomer = await context.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId);
            if (existingCustomer is null)
            {
                await transaction.RollbackAsync();
                return (null, RepairIntakeError.CustomerNotFound);
            }
            customer = existingCustomer;
        }

        Device device;
        if (request.NewDevice is not null)
        {
            var deviceType = await context.DeviceTypes.FirstOrDefaultAsync(x => x.Id == request.NewDevice.DeviceTypeId);
            if (deviceType is null)
            {
                await transaction.RollbackAsync();
                return (null, RepairIntakeError.DeviceTypeNotFound);
            }
            device = new Device
            {
                CustomerId = customer.Id,
                Customer = customer,
                DeviceTypeId = deviceType.Id,
                DeviceType = deviceType,
                Brand = request.NewDevice.Brand.Trim(),
                Model = request.NewDevice.Model.Trim(),
                SerialNumber = Normalize(request.NewDevice.SerialNumber),
                Color = Normalize(request.NewDevice.Color),
                Description = Normalize(request.NewDevice.Description)
            };
            await context.Devices.AddAsync(device);
            await context.SaveChangesAsync();
        }
        else
        {
            var existingDevice = await context.Devices.Include(x => x.DeviceType)
                .FirstOrDefaultAsync(x => x.Id == request.DeviceId && x.CustomerId == customer.Id);
            if (existingDevice is null)
            {
                await transaction.RollbackAsync();
                return (null, RepairIntakeError.DeviceNotFoundForCustomer);
            }
            device = existingDevice;
        }

        var ticket = new RepairTicket
        {
            TicketNumber = RepairTicketNumberGenerator.Generate(),
            CustomerId = customer.Id,
            Customer = customer,
            DeviceId = device.Id,
            Device = device,
            IssueDescription = request.IssueDescription.Trim(),
            CustomerNotes = Normalize(request.CustomerNotes),
            Status = RepairStatus.Received,
            ReceivedAt = DateTime.UtcNow
        };
        await context.RepairTickets.AddAsync(ticket);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (ticket, RepairIntakeError.None);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
