using System.Diagnostics;
using System.Text;

namespace MrMergePdfStamper.Services;

public sealed class ExternalToolsRunner
{
    public async Task RunProcessAsync(string exePath, string arguments, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Не найден исполняемый файл: {exePath}", exePath);
        }

        using var process = new Process();
        process.StartInfo.FileName = exePath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                stdOut.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                stdErr.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Ошибка запуска '{Path.GetFileName(exePath)}' (код {process.ExitCode}).\n" +
                $"Аргументы: {arguments}\n" +
                $"STDOUT: {stdOut}\nSTDERR: {stdErr}");
        }
    }
}
