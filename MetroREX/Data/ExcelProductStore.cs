using ClosedXML.Excel;
using MetroREX.Models;

namespace MetroREX.Data;

internal sealed class ExcelProductStore : IStore<Product>
{
    private const string ProductsSheetName = "Products";
    private const string SpecsSheetName = "Specs";
    private const char ColorSeparator = ';';
    private const double MaxColumnWidth = 60;

    private static readonly (string Header, Func<Product, XLCellValue> Value)[] ProductColumns =
    {
        ("Id", product => product.Id),
        ("Name", product => product.Name),
        ("Category", product => product.Category),
        ("BasePrice", product => product.BasePrice),
        ("CurrentPrice", product => product.CurrentPrice),
        ("Quantity", product => product.Quantity),
        ("QuantityYesterday", product => product.QuantityYesterday),
        ("Description", product => product.Description),
        ("Seller", product => product.Seller),
        ("Colors", product => string.Join(ColorSeparator, product.Colors)),
        ("RatingSum", product => product.RatingSum),
        ("RatingCount", product => product.RatingCount),
    };

    private static readonly string[] RequiredProductColumns = { "Id", "Name", "Category", "BasePrice", "Quantity" };
    private static readonly string[] SpecColumns = { "ProductId", "Name", "Value" };

    private readonly string _path;
    private readonly object _gate = new();

    public ExcelProductStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public List<Product> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                throw new FileNotFoundException("Fisierul cu produse nu a fost gasit.", _path);
            }

            using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet(ProductsSheetName, out var productsSheet))
            {
                throw new InvalidDataException($"Foaia '{ProductsSheetName}' lipseste din '{_path}'.");
            }

            var products = ReadProducts(productsSheet);

            if (workbook.TryGetWorksheet(SpecsSheetName, out var specsSheet))
            {
                ReadSpecs(specsSheet, products);
            }

            return products;
        }
    }

    public void Save(IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        var snapshot = products.ToList();

        lock (_gate)
        {
            using var workbook = new XLWorkbook();
            WriteProducts(workbook.AddWorksheet(ProductsSheetName), snapshot);
            WriteSpecs(workbook.AddWorksheet(SpecsSheetName), snapshot);
            AtomicFile.Write(_path, stream => workbook.SaveAs(stream));
        }
    }

    private static List<Product> ReadProducts(IXLWorksheet sheet)
    {
        var (columns, rows) = ReadLayout(sheet, RequiredProductColumns);
        var products = new List<Product>();
        var knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var reader = new RowReader(sheet.Name, row, columns);

            var id = reader.RequiredText("Id");
            if (!knownIds.Add(id))
            {
                throw reader.Invalid("Id", $"identificatorul '{id}' apare de mai multe ori");
            }

            var basePrice = reader.Decimal("BasePrice");
            var quantity = reader.Int("Quantity");

            products.Add(new Product
            {
                Id = id,
                Name = reader.RequiredText("Name"),
                Category = reader.RequiredText("Category"),
                BasePrice = basePrice,
                CurrentPrice = reader.Decimal("CurrentPrice", basePrice),
                Quantity = quantity,
                QuantityYesterday = reader.Int("QuantityYesterday", quantity),
                Description = reader.Text("Description"),
                Seller = reader.Text("Seller"),
                Colors = SplitColors(reader.Text("Colors")),
                RatingSum = reader.Int("RatingSum", 0),
                RatingCount = reader.Int("RatingCount", 0),
            });
        }

        return products;
    }

    private static void ReadSpecs(IXLWorksheet sheet, List<Product> products)
    {
        var (columns, rows) = ReadLayout(sheet, SpecColumns);
        var productsById = products.ToDictionary(product => product.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var reader = new RowReader(sheet.Name, row, columns);

            if (productsById.TryGetValue(reader.RequiredText("ProductId"), out var product))
            {
                product.Specs.Add(new ProductSpec
                {
                    Name = reader.RequiredText("Name"),
                    Value = reader.Text("Value"),
                });
            }
        }
    }

    private static void WriteProducts(IXLWorksheet sheet, IReadOnlyList<Product> products)
    {
        for (var column = 0; column < ProductColumns.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = ProductColumns[column].Header;

            for (var row = 0; row < products.Count; row++)
            {
                sheet.Cell(row + 2, column + 1).Value = ProductColumns[column].Value(products[row]);
            }
        }

        var priceStart = Array.FindIndex(ProductColumns, c => c.Header == "BasePrice") + 1;
        var priceEnd = Array.FindIndex(ProductColumns, c => c.Header == "CurrentPrice") + 1;
        sheet.Columns(priceStart, priceEnd).Style.NumberFormat.Format = "0.00";

        FormatSheet(sheet, ProductColumns.Length);
    }

    private static void WriteSpecs(IXLWorksheet sheet, IReadOnlyList<Product> products)
    {
        for (var column = 0; column < SpecColumns.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = SpecColumns[column];
        }

        var row = 2;
        foreach (var product in products)
        {
            foreach (var spec in product.Specs)
            {
                sheet.Cell(row, 1).Value = product.Id;
                sheet.Cell(row, 2).Value = spec.Name;
                sheet.Cell(row, 3).Value = spec.Value;
                row++;
            }
        }

        FormatSheet(sheet, SpecColumns.Length);
    }

    private static void FormatSheet(IXLWorksheet sheet, int columnCount)
    {
        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        sheet.SheetView.FreezeRows(1);

        foreach (var column in sheet.Columns(1, columnCount))
        {
            column.AdjustToContents();
            if (column.Width > MaxColumnWidth)
            {
                column.Width = MaxColumnWidth;
            }
        }
    }

    private static (Dictionary<string, int> Columns, IEnumerable<IXLRow> Rows) ReadLayout(
        IXLWorksheet sheet,
        IReadOnlyCollection<string> requiredColumns)
    {
        var rows = sheet.RowsUsed().ToList();
        if (rows.Count == 0)
        {
            throw new InvalidDataException($"Foaia '{sheet.Name}' este goala.");
        }

        var columns = rows[0]
            .CellsUsed()
            .GroupBy(cell => cell.GetString().Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

        var missing = requiredColumns.Where(name => !columns.ContainsKey(name)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException(
                $"Foaia '{sheet.Name}' nu are coloanele obligatorii: {string.Join(", ", missing)}.");
        }

        return (columns, rows.Skip(1));
    }

    private static List<string> SplitColors(string value) =>
        value.Split(ColorSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToList();

    private sealed class RowReader
    {
        private readonly string _sheetName;
        private readonly IXLRow _row;
        private readonly IReadOnlyDictionary<string, int> _columns;

        public RowReader(string sheetName, IXLRow row, IReadOnlyDictionary<string, int> columns)
        {
            _sheetName = sheetName;
            _row = row;
            _columns = columns;
        }

        public string Text(string column) => FindCell(column)?.GetString().Trim() ?? string.Empty;

        public string RequiredText(string column)
        {
            var value = Text(column);
            return value.Length > 0 ? value : throw Invalid(column, "valoare lipsa");
        }

        public decimal Decimal(string column, decimal? fallback = null)
        {
            var cell = FindCell(column);
            if (cell is null)
            {
                return fallback ?? throw Invalid(column, "valoare lipsa");
            }

            if (!cell.TryGetValue(out decimal value) || value < 0)
            {
                throw Invalid(column, "valoare numerica invalida");
            }

            return Math.Round(value, 2);
        }

        public int Int(string column, int? fallback = null)
        {
            var cell = FindCell(column);
            if (cell is null)
            {
                return fallback ?? throw Invalid(column, "valoare lipsa");
            }

            if (!cell.TryGetValue(out int value) || value < 0)
            {
                throw Invalid(column, "numar intreg invalid");
            }

            return value;
        }

        public InvalidDataException Invalid(string column, string reason) =>
            new($"Foaia '{_sheetName}', randul {_row.RowNumber()}, coloana '{column}': {reason}.");

        private IXLCell? FindCell(string column)
        {
            if (!_columns.TryGetValue(column, out var index))
            {
                return null;
            }

            var cell = _row.Cell(index);
            return cell.IsEmpty() ? null : cell;
        }
    }
}
