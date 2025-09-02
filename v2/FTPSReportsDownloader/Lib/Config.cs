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
using System.Configuration;

namespace Lib
{
    public static class Config
    {
        /// <summary>
        /// Чтение значения ключа из файла .config
        /// </summary>
        /// <param name="key">Ключ в appSettings.</param>
        /// <returns>Значение ключа.</returns>
        public static string GetValue(string key) 
            => ConfigurationManager.AppSettings[key];

        /// <summary>
        /// Чтение значения ключа из файла .config
        /// </summary>
        /// <param name="key">Ключ в appSettings.</param>
        /// <param name="now">Дата/время для подстановки в строку формата.</param>
        /// <returns>Значение ключа.</returns>
        public static string GetValue(string key, DateTime now)
            => string.Format(GetValue(key), now);
    }
}
