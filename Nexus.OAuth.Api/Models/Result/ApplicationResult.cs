﻿using FileAccess = Nexus.OAuth.Dal.Models.Enums.FileAccess;

namespace Nexus.OAuth.Api.Models.Result;

public class ApplicationResult
{
    /// <summary>
    /// Application Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Application Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Application Secret
    /// </summary>
    public string Secret { get; set; }

    /// <summary>
    /// Application unique identify key 
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Appplication Redirect Login URL
    /// </summary>
    public string RedirectLogin { get; set; }

    /// <summary>
    /// Application Authorize Login redirect URL
    /// </summary>
    public string RedirectAuthorize { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Application work Status
    /// </summary>
    public ApplicationStatus Status { get; set; }

    /// <summary>
    /// Application Logo
    /// </summary>
    public FileResult Logo { get; set; }

    /// <summary>
    /// Indicates if and an internal Nexus Company application
    /// </summary>
    public bool Internal { get => internalApps.Contains(Id); }

    private int[] internalApps = new int[] { 2 };
    /// <summary>
    /// 
    /// </summary>
    /// <param name="application"></param>
    public ApplicationResult(Application application, Dal.Models.File? file)
    {
        Name = application.Name;
        Id = application.Id;
        Secret = application.Secret;
        Key = application.Key;
        Status = application.Status;
        RedirectLogin = application.RedirectLogin;
        RedirectAuthorize = application.RedirectAuthorize;
        Description = application.Description;
        Logo = new(file ?? new()
        {
            DirectoryType = DirectoryType.Defaults,
            FileName = "example.png",
            Type = FileType.Template,
            Access = FileAccess.Public,
        }, ResourceType.ApplicationLogo);
    }
}

