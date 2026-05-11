namespace PRC.AdminService.DTOs;

public record ContactSubmissionDto(
    string Name,
    string Email,
    string? Phone,
    string Subject,
    string Body);

public record ContactMessageListItem(
    Guid Id,
    string SenderRole,
    string SenderName,
    string SenderEmail,
    string? SenderPhone,
    string Subject,
    string Status,
    bool HasReply,
    DateTime CreatedAt);

public record ContactMessageDetail(
    Guid Id,
    string SenderRole,
    string SenderName,
    string SenderEmail,
    string? SenderPhone,
    string Subject,
    string Body,
    string Status,
    string? AdminReply,
    DateTime? RepliedAt,
    Guid? RepliedByAdminId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record ContactReplyDto(string Reply);
