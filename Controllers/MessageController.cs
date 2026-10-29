
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers
{
    // Only authenticated users can access messaging.
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Messages
        // Displays messages sent or received by the current user.
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Challenge();
            }

            var messages = await _context.Messages
                .Where(m =>
                    m.SenderId == currentUserId ||
                    m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: /Messages/Conversation?userId=...
        // Opens a conversation with another registered user.
        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(userId) ||
                userId == currentUserId)
            {
                return BadRequest("Choose another user to message.");
            }

            var recipient = await _userManager.FindByIdAsync(userId);

            if (recipient == null)
            {
                return NotFound("User not found.");
            }

            // Mark incoming messages as read.
            var unreadMessages = await _context.Messages
                .Where(m =>
                    m.SenderId == userId &&
                    m.ReceiverId == currentUserId &&
                    !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            if (unreadMessages.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            // Load messages exchanged between these users.
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId &&
                     m.ReceiverId == userId) ||
                    (m.SenderId == userId &&
                     m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.OtherUserId = userId;
            ViewBag.OtherUserName = recipient.UserName;

            return View(messages);
        }

        // POST: /Messages/Send
        // Saves a message and notifies its recipient.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            string receiverId,
            string content)
        {
            var senderId = _userManager.GetUserId(User);

            if (senderId == null)
            {
                return Challenge();
            }

            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(receiverId) ||
                receiverId == senderId ||
                string.IsNullOrWhiteSpace(content) ||
                content.Length > 2000)
            {
                TempData["MessageError"] =
                    "Choose a valid recipient and enter a message " +
                    "between 1 and 2000 characters.";

                return RedirectToAction(nameof(Index));
            }

            // Confirm that the recipient is registered.
            var recipient = await _userManager.FindByIdAsync(receiverId);

            if (recipient == null)
            {
                return NotFound("Recipient not found.");
            }

            // Create the message.
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);

            // Get the sender's details for the notification.
            var sender = await _userManager.GetUserAsync(User);

            // Create a notification for the recipient.
            var notification = new Notification
            {
                UserId = receiverId,
                Title = "New Message",
                Message =
                    $"You have received a new message from " +
                    $"{sender?.UserName ?? "another user"}.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);

            // Save the message and notification.
            await _context.SaveChangesAsync();

            // Open the conversation after sending.
            return RedirectToAction(
                nameof(Conversation),
                new { userId = receiverId });
        }
    }
}
