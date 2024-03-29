﻿using QRCoder;
using System.Web;
using Nexus.OAuth.Api.Controllers.Base;
using System.ComponentModel;

namespace Nexus.OAuth.Api.Controllers;


[AllowAnonymous]
[Route("api/Authentications/QrCode")]
public class AuthenticationsQrCodeController : ApiController
{
    public const double MaxQrCodeAge = AuthenticationsController.FirsStepMaxTime;
    public const int MaxPixeisPerModuleQrCode = 150;
    public const int MinKeyLength = AuthenticationsController.MinKeyLength;
    public const int MaxKeyLength = AuthenticationsController.MaxKeyLength;
    public const int AuthenticationTokenSize = AuthenticationsController.AuthenticationTokenSize;

    public AuthenticationsQrCodeController(IConfiguration configuration) : base(configuration)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client_key">Client unique key</param>
    /// <param name="user_agent">Client user agent</param>
    /// <param name="pixeis_per_module">Pixeis per module (<c>Default: 5</c>, <c>Max: 50</c>)</param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    [Route("Generate")]
    [ProducesResponseType(typeof(byte[]), (int)HttpStatusCode.OK, "image/png")]
    public async Task<IActionResult> GetQrCodeAsync([FromHeader(Name = ClientKeyHeader)] string client_key, [FromHeader(Name = UserAgentHeader)] string user_agent, Theme theme = Theme.Dark, bool transparent = true, int? pixeis_per_module = 5)
    {
        if (string.IsNullOrEmpty(client_key) ||
            string.IsNullOrEmpty(user_agent))
            return BadRequest();

        if (client_key.Length < MinKeyLength ||
            client_key.Length > MaxKeyLength)
            return BadRequest();

        if (pixeis_per_module > MaxPixeisPerModuleQrCode)
            pixeis_per_module = MaxPixeisPerModuleQrCode;

        string code = GeneralHelpers.GenerateToken(9, false, false);
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["code"] = code;
        query["registor_key"] = client_key;

        UriBuilder uri = new($"https://{Request.Host}");
        uri.Path = "api/Authentications/QrCode/Authorize";
        uri.Query = query.ToString();
        string url = uri.ToString();
        string validationToken = GeneralHelpers.GenerateToken(AuthenticationTokenSize, false);

        QrCodeReference qrCodeReference = new()
        {
            ClientKey = GeneralHelpers.HashPassword(client_key),
            Code = code,
            Create = DateTime.UtcNow,
            Valid = true,
            IpAdress = RemoteIpAdress?.ToString() ?? string.Empty,
            UserAgent = user_agent,
            ValidationToken = GeneralHelpers.HashPassword(validationToken)
        };

        QRCodeGenerator qrGenerator = new();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] bytes = qrCode.GetGraphic(pixeis_per_module ?? 5,
            GetPrimaryColor(theme),
            new byte[] { 255, 255, 255, (byte)(transparent ? 0 : 255) });

        await db.QrCodes.AddAsync(qrCodeReference);
        await db.SaveChangesAsync();

        Response.Headers["X-Code"] = code;
        Response.Headers["X-Validation"] = validationToken;

        return File(bytes, "image/png");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="registor_key">Client key of registor</param>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("Authorize")]
    [RequireAuthentication]
    public async Task<IActionResult> AuthorizeCodeAsync(string registor_key, string code)
    {
        Account? account = ClientAccount;

        if (account == null)
            return Unauthorized();

        QrCodeReference? codeReference = await (from qrCode in db.QrCodes
                                                where qrCode.Code == code &&
                                                      qrCode.Valid &&
                                                      !qrCode.Used
                                                select qrCode).FirstOrDefaultAsync();
        if (codeReference == null)
            return NotFound();

        if ((DateTime.UtcNow - codeReference.Create).TotalMilliseconds > MaxQrCodeAge)
        {
            codeReference.Valid = false;
            await db.SaveChangesAsync();
        }

        if (!GeneralHelpers.ValidPassword(registor_key, codeReference.ClientKey))
            return NotFound();

        codeReference.Used = true;
        codeReference.Valid = false;
        codeReference.Use = DateTime.UtcNow;

        QrCodeAuthorization codeAuthorization = new()
        {
            AccountId = account.Id,
            QrCodeReferenceId = codeReference.Id,
            AuthorizeDate = DateTime.UtcNow,
            Token = GeneralHelpers.GenerateToken(AuthenticationsController.AuthenticationTokenSize),
            Valid = true
        };

        db.QrCodeAuthorizations.Add(codeAuthorization);

        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientKey"></param>
    /// <param name="code"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [HttpGet]
    [Route("CheckStatus")]
    public async Task<IActionResult> CheckQrCodeStatusAsync([FromHeader(Name = ClientKeyHeader)] string clientKey, string code, string token)
    {
        string adress = RemoteIpAdress?.ToString() ?? string.Empty;

        QrCodeReference? codeReference = (from qrCode in db.QrCodes
                                          where qrCode.Code == code &&
                                                qrCode.IpAdress == adress &&
                                                qrCode.Valid
                                          select qrCode).FirstOrDefault();

        if (codeReference == null)
            return NotFound();


        if ((DateTime.UtcNow - codeReference.Create).TotalMilliseconds > MaxQrCodeAge ||
            codeReference.Used)
        {
            codeReference.Valid = false;
            await db.SaveChangesAsync();
            return NotFound();
        }

        if (!GeneralHelpers.ValidPassword(codeReference.ClientKey, clientKey) &&
            !GeneralHelpers.ValidPassword(codeReference.ValidationToken, token))
        {
            return NotFound();
        }

        throw new NotImplementedException();
    }

    private static byte[] GetPrimaryColor(Theme theme) => theme switch
    {
        Theme.Light => new byte[] { 190, 190, 190 },
        _ => new byte[3]
    };
}

