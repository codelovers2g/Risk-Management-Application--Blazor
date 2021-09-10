 [Route("api/{controller}/{action}/{id?}")]
    public class BoldReportsAPIController : ControllerBase, IReportController
    {
        // Report Viewer requires a memory cache to store the information of consecutive client requests and
        // the rendered Report Viewer in the server.
        private IMemoryCache _cache;

        // IHostingEnvironment used with sample to get the application data from wwwroot.
        private IWebHostEnvironment _hostingEnvironment;
        private IConfiguration _configuration;

        Dictionary<string, object> jsonResult = null;
        public BoldReportsAPIController(IMemoryCache memoryCache, IWebHostEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _cache = memoryCache;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }
        //Get action for getting resources from the report
        [ActionName("GetResource")]
        [AcceptVerbs("GET")]
        // Method will be called from Report Viewer client to get the image src for Image report item.
        public object GetResource(ReportResource resource)
        {
            return ReportHelper.GetResource(resource, this, _cache);
        }

        // Method will be called to initialize the report information to load the report with ReportHelper for processing.
        public void OnInitReportOptions(ReportViewerOptions reportOption)
        {
            // Here, we have loaded the sales-order-detail.rdl report from the application folder wwwroot\Resources. sales-order-detail.rdl should be in the wwwroot\Resources application folder.
            string basePath = _hostingEnvironment.WebRootPath;

            byte[] ReportStream = new UploadController(_configuration).GetFileByteArray(reportOption.ReportModel.ReportPath);
            MemoryStream reportStream = new MemoryStream(ReportStream);
            reportOption.ReportModel.Stream = reportStream;
        }
        // Method will be called when report is loaded internally to start the layout process with ReportHelper.
        public void OnReportLoaded(ReportViewerOptions reportOption)
        {
        }

        [HttpPost]
        public object PostFormReportAction()
        {
            return ReportHelper.ProcessReport(null, this, _cache);
        }

        // Post action to process the report from the server based on json parameters and send the result back to the client.
        [HttpPost]
        public object PostReportAction([FromBody] Dictionary<string, object> jsonArray)
        {
            return ReportHelper.ProcessReport(jsonArray, this, _cache);
        }
    }