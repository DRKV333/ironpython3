﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace IronPythonTest.Util {
    using Section = Dictionary<string, string>;
    using OptionStore = Dictionary<string, Dictionary<string, string>>;

    public class IniParser {
        private OptionStore options;

        public IniParser(Stream source) {
            this.options = Parse(new StreamReader(source).ReadLines());
        }

        public string GetValue(string sectionName, string key) {
            return GetValue(sectionName, key, null);
        }

        public string GetValue(string sectionName, string key, string @default) {
            sectionName = string.IsNullOrEmpty(sectionName) ? "DEFAULT" : sectionName;

            string value;
            while ((sectionName = GetParentSection(sectionName, out Section section)) != null) {
                if (section.TryGetValue(key, out value)) {
                    return value;
                }
            }

            return options["DEFAULT"].TryGetValue(key, out value) ? value : @default;
        }

        private string GetParentSection(string sectionName, out Section section) {
            var idx = sectionName.LastIndexOf('.');
            var newSectionName = idx == -1 ? null : sectionName.Substring(0, idx);
            return options.TryGetValue(sectionName, out section) || newSectionName == null ? newSectionName : GetParentSection(newSectionName, out section);
        }

        public bool GetBool(string sectionName, string key) {
            return GetValue(sectionName, key).AsBool();
        }

        public bool GetBool(string sectionName, string key, bool @default) {
            return GetValue(sectionName, key, @default ? "t" : "f").AsBool();
        }

        public int GetInt(string sectionName, string key) {
            return GetValue(sectionName, key).AsInt();
        }

        public int GetInt(string sectionName, string key, int @default) {
            return GetValue(sectionName, key, @default.ToString()).AsInt();
        }

        public TEnum GetEnum<TEnum>(string sectionName, string key) {
            return this.GetValue(sectionName, key).AsEnum<TEnum>();
        }

        public TEnum GetEnum<TEnum>(string sectionName, string key, TEnum @default) {
            return this.GetValue(sectionName, key, @default.ToString()).AsEnum<TEnum>();
        }

        private static OptionStore Parse(IEnumerable<string> lines) {
            Section currentSection = new Section();
            OptionStore options = new OptionStore(StringComparer.OrdinalIgnoreCase) { { "DEFAULT", currentSection } };
            foreach (var rawline in lines) {
                string line = rawline.Split(new [] { ';', '#' }, 2)[0].Trim();

                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal)) {
                    var sectionName = line.Substring(1, line.Length - 2);
                    if (!options.TryGetValue(sectionName, out currentSection)) {
                        currentSection = new Section();
                        options.Add(sectionName, currentSection);
                    } 
                } else {
                    var result = line.Split(new [] {'='}, 2);
                    var key = result[0].Trim();
                    var value = result.Length > 1 ? result[1] : "1";

                    currentSection.Add(key, value);
                }

            }

            return options;
        }
    }

    internal static class TextReaderExtensions {
        public static IEnumerable<string> ReadLines(this TextReader tr) {
            string line;
            while ((line = tr.ReadLine()) != null) {
                yield return line;
            }
        }
    }

    internal static class StringExtensions {
        private static HashSet<string> Truthy = new HashSet<string> { "1", "t", "true", "y", "yes" };
        private static HashSet<string> Falsey = new HashSet<string> { "0", "f", "false", "n", "no" };

        public static bool AsBool(this string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }

            var l = s.ToLowerInvariant();
            if (Truthy.Contains(l)) {
                return true;
            } else if (Falsey.Contains(l)) {
                return false;
            } else {
                throw new ArgumentException(string.Format("'{0}' is neither true nor false.", s));
            }
        }

        public static int AsInt(this string s) {
            if(s == null) {
                throw new ArgumentNullException(nameof(s));
            }

            return int.Parse(s);
        }

        public static TEnum AsEnum<TEnum>(this string s) {
            return (TEnum)Enum.Parse(typeof(TEnum), s);
        }
    }
}
