﻿using Nexus.OAuth.Api.Controllers.Base;
using Nexus.OAuth.Domain.Storage;
using SixLabors.ImageSharp;
using File = Nexus.OAuth.Dal.Models.File;
using FileAccess = Nexus.OAuth.Dal.Models.Enums.FileAccess;

namespace Nexus.OAuth.Api.Controllers;

[AllowAnonymous]
public class FilesController : ApiController
{
    public FilesController(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// Download as image
    /// </summary>
    /// <param name="fileName">Image filename</param>
    /// <param name="resourceType">Image Resource type</param>
    /// <param name="extension">Result extension</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{type}/Download")]
    public async Task<IActionResult> DownloadAsync(string? type,string fileName, ResourceType resourceType, ImageExtension extension = ImageExtension.Png)
    {
        Account? account = ClientAccount;

        int? accountId = account?.Id;

        File file = await (from fl in db.Files
                           where fl.FileName == fileName &&
                                 fl.Type == FileType.Image &&
                                 (fl.Access == FileAccess.Public || fl.ResourceOwnerId == accountId)
                           select fl).FirstOrDefaultAsync();

        if (file == null)
            return NotFound();

        try
        {
            byte[] fileBytes = await FileStorage.ReadFileAsync(file.Type, DirectoryByResourceType(resourceType), file.FileName);

            if (extension != ImageExtension.Png)
            {
                MemoryStream ms = new(fileBytes);
                Image image;

                using (ms)
                {
                    image = await Image.LoadAsync(ms);
                }

                ms = new();

                switch (extension)
                {
                    case ImageExtension.Jpeg:
                        await image.SaveAsJpegAsync(ms);
                        break;

                    case ImageExtension.Gif:
                        await image.SaveAsGifAsync(ms);
                        break;

                    case ImageExtension.Bmp:
                        await image.SaveAsBmpAsync(ms);
                        break;

                    case ImageExtension.Tga:
                        await image.SaveAsTgaAsync(ms);
                        break;
                }

                fileBytes = await Task.Run(() => ms.ToArray());
                return File(fileBytes, $"image/{Enum.GetName(extension)?.ToLowerInvariant() ?? "png"}");
            }

            return File(fileBytes, "image/png");
        }
        catch (FileNotFoundException ex)
        {
            return BadRequest();
        }
    }

    [NonAction]
    private static DirectoryType DirectoryByResourceType(ResourceType resourceType) => resourceType switch
    {
        ResourceType.ApplicationLogo => DirectoryType.ApplicationsLogo,
        _ => DirectoryType.ApplicationsLogo
    };
}

