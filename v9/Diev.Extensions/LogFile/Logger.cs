#region License
/*
Copyright 2022-2025 Dmitrii Evdokimov
Open source software

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#endregion

using System.Text;
using Diev.Extensions.Tools;
using Path = System.IO.Path;

namespace Diev.Extensions.LogFile;

public static class Logger
{
    private static readonly StringBuilder _lines = new();

    //public static LoggerSettings Settings { get; set; } = new();

    private const int _defaultEncoding = 1251;

    // appsettings.json
    public static string FileNameFormat { get; set; } = @"logs\{0:yyyy-MM}\{0:yyyyMMdd-HHmm}.log";
    public static int FileEncoding { get; set; } = _defaultEncoding;
    public static string LineFormat { get; set; } = @"{0:HH:mm:ss.fff} {1}";
    public static bool LogToConsole { get; set; } = false;
    public static bool AutoFlush { get; set; } = false;

    // internal use
    public static string FileName { get; set; } = "Trace.log";
    public static string LastErrorFileName => Path.Combine(FilePath, "LastError.txt");
    public static string FilePath => Path.GetDirectoryName(Path.GetFullPath(FileName))!;
    public static Encoding EncodingValue { get; set; } = Encoding.GetEncoding(_defaultEncoding);

    public static void Reset()
    {
        _lines.Clear();
        EncodingValue = Encoding.GetEncoding(FileEncoding);

        FileName = Path.GetFullPath(
            string.Format(Environment.ExpandEnvironmentVariables(FileNameFormat), DateTime.Now));
        Directory.CreateDirectory(FilePath); //TODO Exceptions
    }

    /// <summary>
    /// Add "DATE-TIME Text line."
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void TimeLine(string text)
    {
        AppendLine(string.Format(LineFormat, DateTime.Now, text));
    }

    /// <summary>
    /// Add "DATE-TIME ### Text line."
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void TimeZZZLine(string text)
    {
        AppendLine(string.Format(LineFormat, DateTime.Now, "### " + text));
    }

    /// <summary>
    /// Add "DATE-TIME "Path" text line."
    /// </summary>
    /// <param name="path">File or path.</param>
    /// <param name="text">Text line.</param>
    public static void TimeNote(string path, string text)
    {
        TimeLine($"{path.PathQuoted()} {text}");
    }

    /// <summary>
    /// Add "Text line."
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void Line(string text)
    {
        AppendLine(text);
    }

    /// <summary>
    /// Add "Text line."
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void Title(string text)
    {
        AppendLine(Environment.NewLine + "### " + text + Environment.NewLine);
        Flush();
    }

    /// <summary>
    /// Add all and "DATE-TIME Text line." to log file (and console).
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void Write(string text)
    {
        AppendLine(string.Format(LineFormat, DateTime.Now, text));
        Flush();
    }

    /// <summary>
    /// Add all and "DATE-TIME "Path" text line." to log file (and console).
    /// </summary>
    /// <param name="path">File or path.</param>
    /// <param name="text">Text line.</param>
    public static void WriteNote(string path, string text)
    {
        Write($"{path.PathQuoted()} {text}");
    }

    /// <summary>
    /// Add all and "DATE-TIME "Path" text line." to log file (and console).
    /// </summary>
    /// <param name="path">File or path.</param>
    /// <param name="properties">Properties of file or path.</param>
    /// <param name="text">Text line.</param>
    public static void WriteNote(string path, string properties, string text)
    {
        Write($"{path.PathQuoted()}{properties} {text}");
    }

    /// <summary>
    /// Add all and "Text line." to log file (and console).
    /// </summary>
    /// <param name="text">Text line.</param>
    public static void WriteLine(string text)
    {
        AppendLine(text);
        Flush();
    }

    private static void AppendLine(string text)
    {
        if (LogToConsole)
        {
            Console.WriteLine(text);
        }

        _lines.AppendLine(text);

        if (AutoFlush)
        {
            Flush();
        }
    }

    /// <summary>
    /// Write all to log file.
    /// </summary>
    public static void Flush(int skiplines = 0)
    {
        if (_lines.Length > 0)
        {
            for (int i = 0; i < skiplines; i++)
            {
                _lines.AppendLine();
            }

            string lines = _lines.ToString();

            var fi = new FileInfo(FileName);
            fi.Directory?.Create(); //TODO Exceptions
            File.AppendAllTextAsync(FileName, lines, EncodingValue).Wait();

            _lines.Clear();
        }
    }

    /// <summary>
    /// Dump exception to log file.
    /// </summary>
    /// <param name="e">Exception.</param>
    public static void LastError(Exception e)
    {
        bool visible = LogToConsole;

        LogToConsole = true;
        WriteLine("Exception: " + e.Message);

        if (e.InnerException is not null)
        {
            WriteLine("Inner exception: " + e.InnerException.Message);
        }

        LogToConsole = visible;

        var error = $"--- {DateTime.Now} ---{Environment.NewLine}{e}{Environment.NewLine}{e.InnerException}{Environment.NewLine}";

        var fi = new FileInfo(LastErrorFileName);
        fi.Directory?.Create(); //TODO Exceptions
        File.AppendAllTextAsync(LastErrorFileName, error).Wait();
    }

    /// <summary>
    /// Mark log file as OK.
    /// </summary>
    public static void MarkOK()
    {
        string file = FileName;
        string ext = Path.GetExtension(file);
        string newFile = Path.ChangeExtension(file, ".ok" + ext);

        Flush();

        try
        {
            File.Move(file, newFile, true);
            FileName = newFile;
        }
        catch 
        { 
            WriteLine($"Error: rename to {newFile.PathQuoted()} failed!");
        }
    }
}
