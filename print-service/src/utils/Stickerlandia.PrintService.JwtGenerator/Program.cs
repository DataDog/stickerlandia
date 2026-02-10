// See https://aka.ms/new-console-template for more information

#pragma warning disable

using Stickerlandia.PrintService.JwtGenerator;

Console.WriteLine("Hello, Stickerlandia Print Service JWT Generator!");

Console.WriteLine("What user id would you like to use?");
var userId = Console.ReadLine() ?? "test-user";

Console.WriteLine("What is the issuer?");
var issuer = Console.ReadLine();

Console.WriteLine("What is the audience?");
var audience = Console.ReadLine();

using var keyProvider = new RsaKeyProvider();

JwtTokenGenerator.GenerateRsaToken(
    userId,
    ["admin"],
    keyProvider,
    issuer,
    audience);