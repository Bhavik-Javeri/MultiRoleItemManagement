using Microsoft.AspNetCore.Mvc;

namespace ItemManagement.FileUpload
{
    public class FileUploadService
    {
        private readonly string _uploadDirectory;

        public FileUploadService(IConfiguration configuration)
        {
            _uploadDirectory = configuration.GetValue<string>("FileUploadSettings:UploadDirectory");
        }

        public void DeleteFileByUrl(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            try
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_uploadDirectory, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch {
            }
        }
        public async Task<string>? UploadFileAsync(IFormFile file, string uploadPath, string? oldImageUrl = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;
                // Delete old image if provided
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    var oldFileName = Path.GetFileName(oldImageUrl);
                    var oldFilePath = Path.Combine(uploadPath, oldFileName);
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }
                // Ensure the upload directory exists
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate a unique file name and save the file
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the relative URL path for the image
                return $"/images/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                return string.Empty;
                //throw;
            }
        }

        //public IActionResult GetProfilePicture(string fileName)
        //{
        //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
        //    if (System.IO.File.Exists(filePath))
        //    {
        //        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        //        var mimeType = "image/jpeg"; // Adjust MIME type based on file type
        //        return File(fileBytes, mimeType);
        //    }
        //    return NotFound();
        //}
    }
}
