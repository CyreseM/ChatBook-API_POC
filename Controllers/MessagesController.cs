using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.DTOs;
using MyWebApi.Services;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IChatService _chatService;

    public MessagesController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto messageDto)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId))
            return Unauthorized();

        try
        {
            var message = await _chatService.SendMessageAsync(senderId, messageDto);
            return Ok(message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("private/{userId}")]
    public async Task<IActionResult> GetPrivateMessages(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var messages = await _chatService.GetPrivateMessagesAsync(currentUserId, userId, page, pageSize);
        return Ok(messages);
    }

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _chatService.GetGroupMessagesAsync(groupId, page, pageSize);
        return Ok(messages);
    }

    [HttpPut("{messageId}")]
    public async Task<IActionResult> UpdateMessage(int messageId, [FromBody] string newContent)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var message = await _chatService.UpdateMessageAsync(messageId, newContent, userId);
            return Ok(message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _chatService.DeleteMessageAsync(messageId, userId);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _chatService.MarkMessageAsReadAsync(messageId, userId);
        return Ok();
    }
}
