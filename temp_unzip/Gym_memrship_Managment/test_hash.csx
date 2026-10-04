using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.Security;
using Microsoft.AspNetCore.Identity;
using System;

var hasher = new BCryptPasswordHasher();
var defaultHasher = new PasswordHasher<ApplicationUser>();
var user = new ApplicationUser();
var hashed = defaultHasher.HashPassword(user, "Admin@123!");
Console.WriteLine("Hashed PBKDF2: " + hashed);

var result = hasher.VerifyHashedPassword(user, hashed, "Admin@123!");
Console.WriteLine("Result: " + result);
