using System.Globalization;
using System.Text;
using MetroREX.Models;

namespace MetroREX.Data;

internal sealed class CouponFileStore : IStore<Coupon>
{
    private const char Separator = '|';
    private const string DateFormat = "yyyy-MM-dd";
    private const int FieldCount = 4;

    private readonly string _path;
    private readonly object _gate = new();

    public CouponFileStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public List<Coupon> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                return new List<Coupon>();
            }

            var coupons = new List<Coupon>();
            var lines = File.ReadAllLines(_path, Encoding.UTF8);

            for (var index = 0; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                coupons.Add(Parse(lines[index], index + 1));
            }

            return coupons;
        }
    }

    public void Save(IEnumerable<Coupon> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var lines = items.Select(Format).ToList();

        lock (_gate)
        {
            AtomicFile.Write(_path, stream =>
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            });
        }
    }

    private Coupon Parse(string line, int lineNumber)
    {
        var parts = line.Split(Separator);

        var isValid = parts.Length == FieldCount
            && Coupon.IsValidFormat(parts[0])
            && !string.IsNullOrWhiteSpace(parts[1])
            && DateTime.TryParseExact(parts[2], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var percent)
            && percent is >= 1 and <= 100;

        if (!isValid)
        {
            throw new InvalidDataException($"Fisierul '{_path}', linia {lineNumber}: cupon invalid.");
        }

        return new Coupon
        {
            Code = Coupon.NormalizeCode(parts[0]),
            Category = parts[1].Trim(),
            ExpirationDate = DateTime.ParseExact(parts[2], DateFormat, CultureInfo.InvariantCulture),
            DiscountPercent = int.Parse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture),
        };
    }

    private static string Format(Coupon coupon) => string.Join(
        Separator,
        coupon.Code,
        coupon.Category.Replace(Separator, ' '),
        coupon.ExpirationDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        coupon.DiscountPercent.ToString(CultureInfo.InvariantCulture));
}
