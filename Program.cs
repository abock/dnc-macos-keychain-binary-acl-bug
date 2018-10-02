using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Xamarin.Security;

class Program
{
    static void Main (string [] args)
    {
        var processPath = Process
            .GetCurrentProcess ()
            .MainModule
            .FileName;

        var targetFramework = typeof (Program)
            .Assembly.GetCustomAttribute<TargetFrameworkAttribute> ()
            .FrameworkName;

        Console.WriteLine ("    This process path: {0}", processPath);
        Console.WriteLine ("    Target framework:  {0}", targetFramework);

        var secretName = ("dnc-apple-security-regression", $"secret-{args [0]}");

        Keychain.StoreSecret (
            KeychainSecret.Create(
                secretName,
                "super secret value"));

        try {
            if (Keychain.TryGetSecret (secretName, out var secret))
                Console.WriteLine ("      Read secret: {0} = {1}", secretName, secret.GetUtf8StringValue ());
            else
                Console.WriteLine ("      Secret does not exist: {0}", secretName);
        } catch (Exception e) {
            Console.WriteLine ("      Exception reading secret:");
            foreach (var line in e.ToString ().Split ("\n"))
                Console.WriteLine ($"        {line}");
        }
    }
}