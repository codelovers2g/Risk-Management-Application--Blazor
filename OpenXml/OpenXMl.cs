public FileStreamResult ToExcel(IQueryable query)
{
	var columns = GetProperties(query.ElementType);
	var stream = new MemoryStream();

	using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
	{
		var workbookPart = document.AddWorkbookPart();
		workbookPart.Workbook = new Workbook();

		var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
		worksheetPart.Worksheet = new Worksheet();

		var workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
		GenerateWorkbookStylesPartContent(workbookStylesPart);

		var sheets = workbookPart.Workbook.AppendChild(new Sheets());
		var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
		sheets.Append(sheet);

		workbookPart.Workbook.Save();

		var sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

		var headerRow = new Row();

		foreach (var column in columns)
		{
			headerRow.Append(new Cell()
			{
				CellValue = new CellValue(column.Key),
				DataType = new EnumValue<CellValues>(CellValues.String)
			});
		}

		sheetData.AppendChild(headerRow);

		foreach (var item in query)
		{
			var row = new Row();

			foreach (var column in columns)
			{
				var value = GetValue(item, column.Key);
				var stringValue = $"{value}";

				var cell = new Cell();

				var underlyingType = column.Value.IsGenericType &&
					column.Value.GetGenericTypeDefinition() == typeof(Nullable<>) ?
					Nullable.GetUnderlyingType(column.Value) : column.Value;

				var typeCode = Type.GetTypeCode(underlyingType);

				if (typeCode == TypeCode.DateTime)
				{
					if (stringValue != string.Empty)
					{
						cell.CellValue = new CellValue() { Text = DateTime.Parse(stringValue).ToOADate().ToString() };
						cell.StyleIndex = 1U;
					}
				}
				else if (typeCode == TypeCode.Boolean)
				{
					cell.CellValue = new CellValue(stringValue.ToLower());
					cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
				}
				else if (IsNumeric(typeCode))
				{
					cell.CellValue = new CellValue(stringValue);
					cell.DataType = new EnumValue<CellValues>(CellValues.Number);
				}
				else
				{
					cell.CellValue = new CellValue(stringValue);
					cell.DataType = new EnumValue<CellValues>(CellValues.String);
				}

				row.Append(cell);
			}

			sheetData.AppendChild(row);
		}


		workbookPart.Workbook.Save();
	}

	if (stream?.Length > 0)
	{
		stream.Seek(0, SeekOrigin.Begin);
	}

	var result = new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
	result.FileDownloadName = $"{query.ElementType}.xls";

	return result;
}

public FileStreamResult ToCSV(IQueryable query)
{
	var columns = GetProperties(query.ElementType);

	var sb = new StringBuilder();

	foreach (var item in query)
	{
		var row = new List<string>();

		foreach (var column in columns)
		{
			var value = GetValue(item, column.Key);

			row.Add(value != null ? value.ToString() : "");
		}

		sb.AppendLine(string.Join(",", row.ToArray()));
	}


	var result = new FileStreamResult(new MemoryStream(Encoding.Default.GetBytes($"{string.Join(",", columns.Select(c => c.Key))}{Environment.NewLine}{sb.ToString()}")), "text/csv");
	result.FileDownloadName = $"{query.ElementType}.csv";

	return result;
}