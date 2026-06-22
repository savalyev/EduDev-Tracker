using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace EduDev_Tracker.Services.Converters
{
    public class ConversionService: IConversionService
    {
        private readonly ConversionRepository _repo;

        public ConversionService(ConversionRepository repo)
        {
            _repo = repo;
        }

        public NumeralResult ConvertNumeral(string input, int toBase = 16)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new NumeralResult(false, "", "", "", "", null, "Введите число");

            try
            {
                long value;
                var trimmed = input.Trim().ToUpperInvariant();

                bool negative = trimmed.StartsWith('-');

                if (negative) trimmed = trimmed[1..];

                if (trimmed.StartsWith("0X"))
                    value = Convert.ToInt64(trimmed[2..], 16);
                else if (trimmed.StartsWith("0B"))
                    value = Convert.ToInt64(trimmed[2..], 2);
                else if (trimmed.StartsWith("0O") || trimmed.StartsWith("0Q"))
                    value = Convert.ToInt64(trimmed[2..], 8);
                else if (long.TryParse(trimmed, out var parsed))
                    value = parsed;
                else
                    return new NumeralResult(false, "", "", "", "", null,
                        "Не распознан формат числа. Примеры: 255, 0xFF, 0b1010, 0o377");

                if (negative) value = -value;

                string? custom = (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
                    ? ConvertToBase(value, toBase)
                    : null;

                return new NumeralResult(
                    true,
                    Binary: Convert.ToString(value, 2).PadLeft(8, '0'),
                    Octal: Convert.ToString(value, 8),
                    Decimal: value.ToString(),
                    Hex: Convert.ToString(value, 16).ToUpper(),
                    Custom: custom);
            }
            catch (Exception ex)
            {
                return new NumeralResult(false, "", "", "", "", null, $"Ошибка: {ex.Message}");
            }
        }

        private static string ConvertToBase(long value, int toBase)
        {
            if (value == 0) return "0";
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var sb = new StringBuilder();
            long n = Math.Abs(value);
            while (n > 0) { sb.Insert(0, chars[(int)(n % toBase)]); n /= toBase; }
            return (value < 0 ? "-" : "") + sb;
        }

        public ColorResult ConvertColor(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ColorResult(false, "", "", "", 0, 0, 0, 0, 0, 0, "Введите цвет");

            try
            {
                int r, g, b;
                var s = input.Trim();

                if (s.StartsWith("#"))
                {
                    var hex = s.TrimStart('#');
                    if (hex.Length == 3)
                        hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                    r = Convert.ToInt32(hex[..2], 16);
                    g = Convert.ToInt32(hex[2..4], 16);
                    b = Convert.ToInt32(hex[4..6], 16);
                }
                else if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
                {
                    var nums = Regex.Matches(s, @"\d+");
                    r = int.Parse(nums[0].Value);
                    g = int.Parse(nums[1].Value);
                    b = int.Parse(nums[2].Value);
                }
                else if (s.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
                {
                    var nums = Regex.Matches(s, @"[\d.]+");
                    double h = double.Parse(nums[0].Value);
                    double sat = double.Parse(nums[1].Value) / 100;
                    double l = double.Parse(nums[2].Value) / 100;
                    (r, g, b) = HslToRgb(h, sat, l);
                }
                else
                {
                    return new ColorResult(false, "", "", "", 0, 0, 0, 0, 0, 0,
                        "Неизвестный формат. Используй #HEX, rgb(...) или hsl(...)");
                }

                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                var (hh, ss, ll) = RgbToHsl(r, g, b);

                return new ColorResult(
                    true,
                    Hex: $"#{r:X2}{g:X2}{b:X2}",
                    Rgb: $"rgb({r}, {g}, {b})",
                    Hsl: $"hsl({hh:F0}, {ss * 100:F1}%, {ll * 100:F1}%)",
                    R: r, G: g, B: b,
                    H: hh, S: ss * 100, L: ll * 100);
            }
            catch (Exception ex)
            {
                return new ColorResult(false, "", "", "", 0, 0, 0, 0, 0, 0, $"Ошибка: {ex.Message}");
            }
        }

        private static (double h, double s, double l) RgbToHsl(int r, int g, int b)
        {
            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double l = (max + min) / 2;
            if (max == min) return (0, 0, l);
            double d = max - min;
            double s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            double h;
            if (max == rd) h = (gd - bd) / d + (gd < bd ? 6 : 0);
            else if (max == gd) h = (bd - rd) / d + 2;
            else h = (rd - gd) / d + 4;
            return (h * 60, s, l);
        }

        private static (int r, int g, int b) HslToRgb(double h, double s, double l)
        {
            if (s == 0) { int v = (int)(l * 255); return (v, v, v); }
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            double Hue(double t)
            {
                if (t < 0) t += 1; if (t > 1) t -= 1;
                if (t < 1.0 / 6) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2) return q;
                if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
                return p;
            }
            double hNorm = h / 360;
            return ((int)(Hue(hNorm + 1.0 / 3) * 255),
                    (int)(Hue(hNorm) * 255),
                    (int)(Hue(hNorm - 1.0 / 3) * 255));
        }

        public ConversionResult ConvertTime(string input, string fromZoneId, string toZoneId)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConversionResult(false, "", "Введите время");

            try
            {
                if (!DateTime.TryParse(input, out var dt))
                    return new ConversionResult(false, "", "Не удалось разобрать дату/время");

                var fromZone = TimeZoneInfo.FindSystemTimeZoneById(fromZoneId);
                var toZone = TimeZoneInfo.FindSystemTimeZoneById(toZoneId);

                var utc = TimeZoneInfo.ConvertTimeToUtc(dt, fromZone);
                var result = TimeZoneInfo.ConvertTimeFromUtc(utc, toZone);

                var offset = toZone.GetUtcOffset(result);
                var sign = offset >= TimeSpan.Zero ? "+" : "";
                var output = $"{result:dd.MM.yyyy HH:mm} (UTC{sign}{offset:hh\\:mm})";

                return new ConversionResult(true, output);
            }
            catch (TimeZoneNotFoundException)
            {
                return new ConversionResult(false, "", $"Часовой пояс не найден: {fromZoneId} / {toZoneId}");
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, "", ex.Message);
            }
        }

        public ConversionResult FormatJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConversionResult(false, "", "Введите JSON");

            try
            {
                var doc = JsonDocument.Parse(input);
                var pretty = JsonSerializer.Serialize(doc.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
                return new ConversionResult(true, pretty);
            }
            catch (JsonException ex)
            {
                return new ConversionResult(false, "", $"Ошибка JSON: {ex.Message}");
            }
        }

        public ConversionResult FormatXml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConversionResult(false, "", "Введите XML");
            try
            {
                var doc = XDocument.Parse(input);
                return new ConversionResult(true, doc.ToString());
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, "", $"Ошибка XML: {ex.Message}");
            }
        }

        public ConversionResult JsonToXml(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new ConversionResult(false, "", "Введите JSON");
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = JsonNodeToXml("root", doc.RootElement);
                return new ConversionResult(true, root.ToString());
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, "", ex.Message);
            }
        }

        private static XElement JsonNodeToXml(string tagName, JsonElement element)
        {
            var name = XmlConvert.EncodeLocalName(
                string.IsNullOrEmpty(tagName) ? "_" : tagName);
            var el = new XElement(name);
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                        el.Add(JsonNodeToXml(prop.Name, prop.Value));
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                        el.Add(JsonNodeToXml("item", item));
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    el.Value = element.ToString();
                    break;
            }
            return el;
        }

        public ConversionResult XmlToJson(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return new ConversionResult(false, "", "Введите XML");
            try
            {
                var doc = XDocument.Parse(xml);
                var obj = XmlNodeToJson(doc.Root!);
                var json = JsonSerializer.Serialize(obj,
                    new JsonSerializerOptions { WriteIndented = true });
                return new ConversionResult(true, json);
            }
            catch (Exception ex)
            {
                return new ConversionResult(false, "", ex.Message);
            }
        }

        private static object? XmlNodeToJson(XElement element)
        {
            var children = element.Elements().ToList();
            if (children.Count == 0)
                return element.Value;

            var dict = new Dictionary<string, object?>();
            foreach (var group in children.GroupBy(e => e.Name.LocalName))
            {
                var items = group.Select(e => XmlNodeToJson(e)).ToList();
                dict[group.Key] = items.Count == 1 ? items[0] : (object)items;
            }
            return dict;
        }

        public ConversionResult UrlEncode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConversionResult(false, "", "Введите текст");

            return new ConversionResult(true, Uri.EscapeDataString(input));
        }

        public ConversionResult UrlDecode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new ConversionResult(false, "", "Введите URL");
            try
            {
                return new ConversionResult(true, Uri.UnescapeDataString(input));
            }
            catch
            {
                return new ConversionResult(false, "", "Не удалось декодировать URL");
            }
        }

        public Task<List<ConversionHistory>> GetHistoryAsync(int profileId, int limit = 50)
            => _repo.GetRecentAsync(profileId, limit);

        public Task<List<ConversionHistory>> GetHistoryByTypeAsync(int profileId, ConversionType type)
            => _repo.GetByTypeAsync(profileId, type);

        public Task SaveToHistoryAsync(int profileId, ConversionType type,
            string input, string output, string? metaJson = null)
                => _repo.AddAsync(new ConversionHistory
                {
                    ProfileId = profileId,
                    Type = type,
                    InputText = input.Length > 200 ? input[..200] + "…" : input,
                    OutputText = output.Length > 200 ? output[..200] + "…" : output,
                    MetadataJson = metaJson,
                    CreatedAt = DateTime.UtcNow
                });

        public Task ClearTodayHistoryAsync(int profileId)
            => _repo.ClearTodayAsync(profileId);

        public Task ClearAllHistoryAsync(int profileId)
            => _repo.ClearAllAsync(profileId);
    }
}
