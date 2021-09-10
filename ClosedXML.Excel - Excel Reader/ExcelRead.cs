  public async void ReadXLSFile(InputFileChangeEventArgs e)
    {
        Loading = "block";
        Exists = true;
        IBrowserFile file = e.File;
        DataTable dtTable = new DataTable();
        try
        {
            var extnsn = Path.GetExtension(file.Name.ToLower());
            if (!string.IsNullOrEmpty(file.Name) && !string.IsNullOrEmpty(extnsn) && ExtensionOfXlsFile.Equals(extnsn))
            {
                MemoryStream myBlob = new MemoryStream();
                await e.File.OpenReadStream().CopyToAsync(myBlob);

                using (XLWorkbook workBook = new XLWorkbook(myBlob))
                {
                    //Read the first Sheet from Excel file.
                    IXLWorksheet workSheet = workBook.Worksheet(1);

                    //Loop through the Worksheet rows.
                    bool firstRow = true;
                    foreach (IXLRow row in workSheet.Rows())
                    {
                        //Use the first row to add columns to DataTable.
                        if (firstRow)
                        {
                            foreach (IXLCell cell in row.Cells())
                            {
                                dtTable.Columns.Add(cell.Value.ToString());
                            }
                            firstRow = false;
                            if (dtTable.Columns.Count < 22)
                            {
                                Exists = false;
                                DataList = null;
                                SupplierDataList = null;
                                Loading = "none";
                                StateHasChanged();
                                return;
                            }
                        }
                        else
                        {
                            //Add rows to DataTable.
                            dtTable.Rows.Add();
                            int i = 0;
                            if (row.FirstCellUsed() != null)
                            {
                                foreach (IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                                {
                                    dtTable.Rows[dtTable.Rows.Count - 1][i] = cell.Value.ToString();
                                    i++;
                                }
                            }
                        }
                    }
                }

                
                    List<FactoriesVm> FDataList;
                    try
                    {
                        FDataList = DataTableToFactoriesList(dtTable);
                        DataList = FDataList;
                        SupplierDataList = null;
                        Loading = "none";
                        StateHasChanged();
                    }
                    catch
                    {
                        Exists = false;
                        Loading = "none";
                        SupplierDataList = null;
                        DataList = null;
                        StateHasChanged();
                    }
            }
            else
            {
                Exists = false;
                InvalidText = "Please select a valid file";
                Loading = "none";
                SupplierDataList = null;
                DataList = null;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            var Ex = ex.Message;
            Exists = false;
            InvalidText = ex.Message;
            Loading = "none";
            SupplierDataList = null;
            DataList = null;
            StateHasChanged();
        }
    }