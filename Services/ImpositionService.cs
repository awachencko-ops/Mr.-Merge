using System.Text;
using MrMergePdfStamper.Models;

namespace MrMergePdfStamper.Services;

public sealed class ImpositionService
{
    private readonly ExternalToolsRunner _runner;

    public ImpositionService(ExternalToolsRunner runner)
    {
        _runner = runner;
    }

    public async Task<string> BuildAsync(
        string inputFile,
        string outputFile,
        IReadOnlyList<PrintJobItem> rows,
        string qpdfPath,
        string gsPath,
        double rightOffsetMm,
        double topOffsetMm,
        string fontName,
        double fontSize,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("Исходный PDF не найден.", inputFile);
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Таблица тиража пуста.");
        }

        var tempRoot = Path.Combine(Path.GetTempPath(), $"pdf-imposition-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var stampedFiles = new List<(string file, int copies)>();

            for (var i = 0; i < rows.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = rows[i];
                var spreadNumber = i + 1;

                if (row.SourcePage < 1 || row.Copies < 1)
                {
                    throw new InvalidOperationException($"Некорректные данные в строке {spreadNumber}: страница и копии должны быть > 0.");
                }

                var tempPage = Path.Combine(tempRoot, $"temp_{spreadNumber}.pdf");
                var stampedPage = Path.Combine(tempRoot, $"stamped_{spreadNumber}.pdf");

                progress?.Report($"Обработка спуска {spreadNumber} из {rows.Count}: извлечение страницы...");
                await _runner.RunProcessAsync(
                    qpdfPath,
                    $"--empty --pages \"{inputFile}\" {row.SourcePage} -- \"{tempPage}\"",
                    cancellationToken);

                progress?.Report($"Обработка спуска {spreadNumber} из {rows.Count}: наложение штампа...");
                var psCode = StampPostScriptBuilder.Generate(spreadNumber, rightOffsetMm, topOffsetMm, fontName, fontSize);
                await _runner.RunProcessAsync(
                    gsPath,
                    $"-dBATCH -dNOPAUSE -q -sDEVICE=pdfwrite -sOutputFile=\"{stampedPage}\" -c \"{psCode}\" -f \"{tempPage}\"",
                    cancellationToken);

                stampedFiles.Add((stampedPage, row.Copies));
            }

            progress?.Report("Финальная сборка PDF...");
            var finalArgs = new StringBuilder("--empty --pages ");
            foreach (var (file, copies) in stampedFiles)
            {
                for (var i = 0; i < copies; i++)
                {
                    finalArgs.Append($"\"{file}\" 1 ");
                }
            }

            finalArgs.Append($"-- \"{outputFile}\"");
            await _runner.RunProcessAsync(qpdfPath, finalArgs.ToString(), cancellationToken);

            progress?.Report("Готово.");
            return outputFile;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
}
