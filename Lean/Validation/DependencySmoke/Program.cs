using System.IO.Compression;
using NetMQ;
using NetMQ.Sockets;

var root = Path.Combine(Path.GetTempPath(), "lean-security-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var count = 0;
    foreach (var overwrite in new[] { false, true })
    foreach (var entry in new[] { "../escaped.txt", "../output-sibling/escaped.txt", Path.Combine(root, "absolute.txt") })
    {
        var archive = Path.Combine(root, $"attack-{count++}.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(zip.CreateEntry(entry).Open())) writer.Write("attack");
        if (QuantConnect.Compression.Unzip(archive, Path.Combine(root, "output"), overwrite))
            throw new Exception("Traversal accepted: " + entry);
    }
    if (File.Exists(Path.Combine(root, "escaped.txt")) || File.Exists(Path.Combine(root, "absolute.txt")) || Directory.Exists(Path.Combine(root, "output-sibling")))
        throw new Exception("An entry escaped extraction root");
    var safe = Path.Combine(root, "safe.zip");
    using (var zip = ZipFile.Open(safe, ZipArchiveMode.Create))
    using (var writer = new StreamWriter(zip.CreateEntry("nested/test.txt").Open())) writer.Write("safe");
    var destination = Path.Combine(root, "normal");
    if (!QuantConnect.Compression.Unzip(safe, destination)) throw new Exception("Normal extraction failed");
    File.WriteAllText(Path.Combine(destination, "nested/test.txt"), "old");
    if (!QuantConnect.Compression.Unzip(safe, destination, true) || File.ReadAllText(Path.Combine(destination, "nested/test.txt")) != "safe")
        throw new Exception("Overwrite failed");
    Console.WriteLine("PASS: six traversal cases and normal/overwrite extraction");
    using (var push = new PushSocket())
    using (var pull = new PullSocket())
    {
        var endpoint = "inproc://lean-smoke-" + Guid.NewGuid().ToString("N");
        pull.Bind(endpoint);
        push.Connect(endpoint);
        push.SendFrame("dependency-check");
        if (!pull.TryReceiveFrameString(TimeSpan.FromSeconds(5), out var message) || message != "dependency-check")
            throw new Exception("NetMQ round-trip failed");
    }
    NetMQConfig.Cleanup(false);
    Console.WriteLine("PASS: NetMQ in-process message round-trip");
}
finally { Directory.Delete(root, true); }
