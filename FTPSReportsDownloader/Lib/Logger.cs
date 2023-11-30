#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Open source software https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Text;

namespace Lib
{
    /// <summary>
    /// Вывод на консоль и запись в файл лога.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Кодировка файла лога.
        /// </summary>
        public static Encoding Encoding { get; set; } = Encoding.GetEncoding(1251);

        /// <summary>
        /// Формат штампа времени.
        /// </summary>
        public static string Format { get; set; } = "yyyy-MM-dd HH:mm:ss ";

        /// <summary>
        /// Имя файла для записи лога.
        /// </summary>
        public static string Log { get; set; } = "log.txt";

        /// <summary>
        /// Записать на консоль и в лог строку текста.
        /// </summary>
        /// <param name="text">Строка для вывода.</param>
        public static void Write(string text)
        {
            Console.Write(text);

            try
            {
                File.AppendAllText(Log, text, Encoding);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: Не могу записать лог файл.");
                Console.WriteLine("Детали ошибки: " + ex.Message + Environment.NewLine);
                Console.WriteLine("Подсказка: Проверьте настройку параметра 'DownloadLog' в конфиг. файле.");
            }
        }

        /// <summary>
        /// Записать на консоль и в лог строку текста с переводом строки.
        /// </summary>
        /// <param name="text">Строка для вывода.</param>
        public static void WriteLine(string text)
            => Write(text + Environment.NewLine);

        /// <summary>
        /// Записать на консоль и в лог строку текста со штампом времени.
        /// </summary>
        /// <param name="text">Строка для вывода.</param>
        public static void TWrite(string text)
            => Write(DateTime.Now.ToString(Format) + text);

        /// <summary>
        /// Записать на консоль и в лог строку текста со штампом времени и переводом строки.
        /// </summary>
        /// <param name="text">Строка для вывода.</param>
        public static void TWriteLine(string text)
            => TWrite(text + Environment.NewLine);
    }
}
