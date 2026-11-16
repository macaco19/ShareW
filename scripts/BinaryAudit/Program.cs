using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
var folder = Path.GetFullPath(args[0]);
var names = new[] { "shareW", "ShareX.Avalonia", "ShareX.HelpersLib", "ShareX.Icons", "ShareX.ImageEditor", "ShareX.ScreenCaptureLib" };
var forbidden = new HashSet<string> { "System.Net.Http.HttpClient", "System.Net.WebClient", "System.Net.WebRequest", "System.Net.FtpWebRequest", "System.Net.Sockets.Socket", "System.Net.Sockets.TcpClient", "System.Net.Sockets.UdpClient", "System.Net.Mail.SmtpClient", "System.Net.NetworkInformation.Ping", "System.Diagnostics.ProcessStartInfo" };
foreach (var name in names)
{
    using var stream = File.OpenRead(Path.Combine(folder, name + ".dll"));
    using var pe = new PEReader(stream);
    var metadata = pe.GetMetadataReader();
    foreach (var handle in metadata.AssemblyReferences)
    {
        var reference = metadata.GetAssemblyReference(handle);
        var dependency = metadata.GetString(reference.Name);
        if (dependency.Contains("Uploaders") || dependency.Contains("FluentFTP") || dependency.Contains("SshNet"))
            throw new Exception(name + " references forbidden assembly " + dependency);
    }
    foreach (var handle in metadata.TypeReferences)
    {
        var reference = metadata.GetTypeReference(handle);
        var type = metadata.GetString(reference.Namespace) + "." + metadata.GetString(reference.Name);
        if (forbidden.Contains(type)) throw new Exception(name + " references forbidden transport/process type " + type);
    }
    if (name == "ShareX.ImageEditor")
    {
        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            var methodName = metadata.GetString(method.Name);
            if (methodName is "Upload" or "get_UploadCommand" or "add_UploadRequested" or "set_UploadImageRequested")
                throw new Exception("Upload hook retained: " + methodName);
        }
    }
    Console.WriteLine("PASS: built assembly " + name + " has no forbidden assembly/type references or upload hooks checked by this audit.");
}
Console.WriteLine("LIMIT: metadata checks do not test indirect calls, third-party implementations or Windows runtime traffic.");
