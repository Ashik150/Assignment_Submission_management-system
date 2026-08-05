using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Backend.Services;

public sealed record StoredSubmissionPdf(string FileId, string FileName, long FileSize);

public sealed class SubmissionPdfService
{
    public const long MaximumFileSize = 10 * 1024 * 1024;

    private readonly GridFSBucket bucket;

    public SubmissionPdfService(IMongoDatabase database)
    {
        bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = "submission_pdfs"
        });
    }

    public async Task<StoredSubmissionPdf> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidDataException("The selected PDF is empty.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new InvalidDataException("The PDF must be 10 MB or smaller.");
        }

        var fileName = Path.GetFileName(file.FileName);
        if (fileName.Length == 0 || fileName.Length > 255 ||
            !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Only files with a .pdf extension are allowed.");
        }

        await using var stream = file.OpenReadStream();
        var signature = new byte[5];
        var bytesRead = await stream.ReadAsync(signature, cancellationToken);
        if (bytesRead != signature.Length ||
            signature[0] != '%' || signature[1] != 'P' || signature[2] != 'D' ||
            signature[3] != 'F' || signature[4] != '-')
        {
            throw new InvalidDataException("The uploaded file is not a valid PDF.");
        }

        stream.Position = 0;
        var fileId = await bucket.UploadFromStreamAsync(
            fileName,
            stream,
            new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    ["contentType"] = "application/pdf",
                    ["size"] = file.Length
                }
            },
            cancellationToken);

        return new StoredSubmissionPdf(fileId.ToString(), fileName, file.Length);
    }

    public Task<byte[]> Download(string fileId, CancellationToken cancellationToken) =>
        bucket.DownloadAsBytesAsync(ObjectId.Parse(fileId), cancellationToken: cancellationToken);

    public async Task Delete(string? fileId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(fileId, out var id))
        {
            return;
        }

        try
        {
            await bucket.DeleteAsync(id, cancellationToken);
        }
        catch (GridFSFileNotFoundException)
        {
            // The database record is already clear, so a missing GridFS object needs no recovery.
        }
    }
}
