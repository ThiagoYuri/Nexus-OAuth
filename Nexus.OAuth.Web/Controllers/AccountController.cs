﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nexus.OAuth.Web.Controllers.Base;
using Nexus.OAuth.Web.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nexus.OAuth.Web.Controllers;

public class AccountController : BaseController
{
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILogger<AccountController> logger)
    {
        _logger = logger;
    }

    public IActionResult Register(string? redirect)
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(Account account)
    {
        if (!ModelState.IsValid)
            return View(account);

        HttpClient client = ApiClient;

       HttpResponseMessage response = await client.PostAsJsonAsync("Accounts/Register",account);

        return Ok();
    }
}