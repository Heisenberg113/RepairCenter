using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RepairCenter.DTOs.Customers;
using RepairCenter.Entities;
using RepairCenter.Services.Interfaces;

namespace RepairCenter.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,Receptionist")]
public class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetAll()
    {
        var customers = await service.GetAllAsync();
        return Ok(customers.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await service.GetByIdAsync(id);
        return customer is null ? NotFound() : Ok(ToResponse(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address
        };

        await service.CreateAsync(customer);
        var response = ToResponse(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, UpdateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address
        };

        var updated = await service.UpdateAsync(id, customer);
        return updated is null ? NotFound() : Ok(ToResponse(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await service.DeleteAsync(id) ? NoContent() : NotFound();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không thể xóa khách hàng",
                Detail = "Khách hàng đang được tham chiếu bởi thiết bị hoặc phiếu sửa chữa."
            });
        }
    }

    private static CustomerResponse ToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        Address = customer.Address,
        CreatedAt = customer.CreatedAt
    };
}
