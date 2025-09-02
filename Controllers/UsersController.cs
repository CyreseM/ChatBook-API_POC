using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using MyWebApi.DTOs;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ApplicationDbContext _context;

    public UsersController(IChatService chatService, ApplicationDbContext context)
    {
        _chatService = chatService;
        _context = context;
    }

    [HttpGet("online")]
    public async Task<IActionResult> GetOnlineUsers()
    {
        var users = await _chatService.GetOnlineUsersAsync();
        return Ok(users);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query is required");

        var users = await _context.Users
            .Where(u => u.FirstName.Contains(query) ||
                       u.LastName.Contains(query) ||
                       u.Email.Contains(query))
            .Take(20)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePicture = u.ProfilePicture,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("All-Users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
                                    .Select(u => new UserDto
                                    {
                                        Id = u.Id,
                                        Email = u.Email,
                                        FirstName = u.FirstName,
                                        LastName = u.LastName,
                                        ProfilePicture = u.ProfilePicture,
                                        IsOnline = u.IsOnline,
                                        LastSeen = u.LastSeen
                                    })
                                    .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                ProfilePicture = u.ProfilePicture,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }
}