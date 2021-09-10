BlobContainerClient GetReportClient()
{
	try
	{
		var storageConnectionString = configuration.GetConnectionString(connectionName);
		var blobServiceClient = new BlobServiceClient(storageConnectionString);
		return blobServiceClient.GetBlobContainerClient(configuration[reportContainerKey]);
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex);
		throw;
	}
}
		
[Route("get")]
[HttpGet]
public async Task<IActionResult> Get(string fileName, string contentType = "application/pdf")
{
	try
	{
		var reportClient = GetReportClient();
		var fileClient = reportClient.GetBlobClient(fileName);
		var reportBlobResult = await fileClient.DownloadAsync();
		var memoryStream = new MemoryStream();
		reportBlobResult.Value.Content.CopyTo(memoryStream);
		Response.Headers["Content-Disposition"] = $"inline;filename={fileName}";
		return new FileContentResult(memoryStream.ToArray(), contentType);
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex);
		return BadRequest("Could not retrieve a report.");
	}
}

[Route("SendDataToBlobWithFolderName")]
[HttpPost]
public async Task<ActionResult<string>> SendDataToBlobWithFolderName(IFormFile file, string assessmentPage, string reportType, string folderName)
{

	if (file?.FileName != null && file.FileName.ToUpperInvariant().EndsWith(reportExtensionUppercase))
	{
		var reportIdString = file.FileName.Substring(0, file.FileName.Length - reportExtensionUppercase.Length);
		var normalizedFileName = string.Empty;
		if (string.IsNullOrWhiteSpace(assessmentPage))
			normalizedFileName = $"{report}/{Assessment}/{folderName}/{reportType}/{reportIdString}{reportExtensionUppercase}";
		else
			normalizedFileName = $"{report}/{Assessment}/{folderName}/{reportType}/{assessmentPage}/{reportIdString}{reportExtensionUppercase}";
		//Delete File If Exists
		DeleteFileIfExists(normalizedFileName);
		try
		{
			var reportClient = GetReportClient();
			using (var fileStream = file.OpenReadStream())
			{
				await reportClient.UploadBlobAsync(normalizedFileName, fileStream);
			}
			return Ok(normalizedFileName);
		}
		catch
		{
			return Conflict("Couldn't add a report. A likely reason for this error message is that the file was already added before.");
		}
	}
	else
	{
		return BadRequest("Please choose a PDF file.");
	}
}