﻿using Nexus.Tools.Validations.Attributes;

namespace Nexus.OAuth.Web.Models
{
    public class Account
    {
        [Name]
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(500, MinimumLength = 5)]
        public string Email { get; set; }

        [Phone]
        [Required]
        [StringLength(21,MinimumLength = 5)]
        public string PhoneNumber { get; set; }

        [Required]
        [Password]
        [StringLength(12, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        [Required]
        [Boolean(TrueOnly = true)]
        public bool AcceptTerms { get; set; }
    }
}
