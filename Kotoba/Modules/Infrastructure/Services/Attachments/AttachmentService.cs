using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Enums;
using Kotoba.Modules.Domain.Interfaces;
using Kotoba.Modules.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Infrastructure.Services.Attachments
{
    public class AttachmentService : IAttachmentService
    {
        private readonly KotobaDbContext _context;
        private readonly string _uploadBasePath;

        public AttachmentService(KotobaDbContext context, IConfiguration configuration)
        {
            _context = context;


            _uploadBasePath = configuration["Attachments:UploadPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        }

        private static FileType ResolveFileType(string contentType) => contentType switch
        {
            var ct when ct.StartsWith("image/") => FileType.Image,
            var ct when ct.StartsWith("video/") => FileType.Video,
            var ct when ct.StartsWith("audio/") => FileType.Audio,
            var ct when ct.StartsWith("document/") => FileType.Document,
            _ => FileType.Document
        };

        public async Task<AttachmentDto?> UploadAttachmentAsync(UploadAttachmentRequest request)
        {
            var messageExists = await _context.Messages
                .AnyAsync(m => m.Id == request.MessageId && !m.IsDeleted);

            if (!messageExists) return null;

            Directory.CreateDirectory(_uploadBasePath);

            var safeOriginalName = Path.GetFileName(request.FileName);
            var extension = Path.GetExtension(safeOriginalName);
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadBasePath, storedFileName);

            long fileSize;
            await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await request.FileStream.CopyToAsync(fs);
                fileSize = fs.Length;
            }

            // Public URL served by the static files middleware: app.UseStaticFiles()
            // Requires wwwroot/uploads (or a custom StaticFileOptions path mapping)
            var fileUrl = $"/uploads/{storedFileName}";

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                MessageId = request.MessageId,
                FileName = safeOriginalName,
                FileType = ResolveFileType(request.ContentType),
                FileUrl = fileUrl,
                FileSizeBytes = fileSize,
                UploadedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return new AttachmentDto
            {
                AttachmentId = attachment.Id,
                MessageId = attachment.MessageId,
                FileName = attachment.FileName,
                FileType = attachment.FileType,
                FileUrl = attachment.FileUrl
            };
        }

        public async Task<List<AttachmentDto>> GetAttachmentsAsync(Guid messageId)
        {
            return await _context.Attachments
                .Where(a => a.MessageId == messageId)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new AttachmentDto
                {
                    AttachmentId = a.Id,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    FileType = a.FileType,
                    FileUrl = a.FileUrl
                })
                .ToListAsync();
        }

    }
}
