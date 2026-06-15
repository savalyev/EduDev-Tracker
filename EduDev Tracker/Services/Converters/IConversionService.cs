using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Converters
{
    public record ConversionResult(bool Success, string Output, string? Error = null);
    public record NumeralResult(
        bool Success,
        string Binary, string Octal, string Decimal, string Hex,
        string? Custom, string? Error = null);
    public record ColorResult(
        bool Success,
        string Hex, string Rgb, string Hsl,
        int R, int G, int B,
        double H, double S, double L,
        string? Error = null);

    public interface IConversionService
    {
        NumeralResult ConvertNumeral(string input, int toBase = 16);

        ColorResult ConvertColor(string input);

        ConversionResult ConvertTime(string input, string fromZoneId, string toZoneId);

        ConversionResult FormatJson(string input);
        ConversionResult FormatXml(string input);
        ConversionResult JsonToXml(string json);
        ConversionResult XmlToJson(string xml);

        ConversionResult UrlEncode(string input);
        ConversionResult UrlDecode(string input);

        Task<List<ConversionHistory>> GetHistoryAsync(int profileId, int limit = 50);
        Task<List<ConversionHistory>> GetHistoryByTypeAsync(int profileId, ConversionType type);
        Task SaveToHistoryAsync(int profileId, ConversionType type,
            string input, string output, string? metaJson = null);
        Task ClearTodayHistoryAsync(int profileId);
        Task ClearAllHistoryAsync(int profileId);
    }
}
