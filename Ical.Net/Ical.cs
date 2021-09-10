private async void ImportCalendarData()
    {
        byte[] responseData = new WebClient().DownloadData("https://p50-caldav.icloud.com/published/2/MTAzODMwMDkwMzYxMDM4M0R3KeEP0fsb9x6PYY2qALVeziQ1MAgqC2dra0B-WZbg");

        string utfString = Encoding.UTF8.GetString(responseData, 0, responseData.Length);

        MemoryStream reportStream = new(responseData);
        Calendar calendarEvent;
        try
        {
            calendarEvent = Calendar.Load(utfString);
            var Events = calendarEvent.Events;

            foreach (ICalendarObject item in calendarEvent.Children)
            {
                if (item is CalendarEvent cv)
                {
                    var Description = cv.Description;
                    var Start = cv.Start != null ? cv.Start.ToString() : null;
                    var End = cv.End != null ? cv.End.ToString() : null;
                    var Location = cv.Location != null ? cv.Location.ToString() : null;
                    var Created = cv.Created != null ? cv.Created.ToString() : null;
                    var Summary = cv.Summary;
                    var Uid = cv.Uid;
                    var LastModified = cv.LastModified != null ? cv.LastModified.ToString() : null;

                    await databaseService.UpdateiCouldCalendarAsync(appState.UserId, Description, Start, End, Location, Created, Summary, Uid, LastModified);
                }
            }
        }
        catch (Exception e)
        {
            var Error = e.Message;
        }
    }