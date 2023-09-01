﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FurnitureStore.Shared.DTOs
{
    public class UserRegistrationRequestDto
    {
        [Required]
        [MaxLength(250,ErrorMessage = "Name can't have more than 250 characters")]
        public string Name { get; set; }
        [Required]
        public string EmailAddress { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
