using System.Globalization;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.XtraPrinting;
using MainDemo.Module.BusinessObjects;
using MainDemo.WebAPI.TestInfrastructure;
using Xunit;

namespace MainDemo.WebAPI.Tests;

public class ReportTests : BaseWebApiTest {
    string DownloadUrl => "/api/Report/DownloadByName";
    string userName = "John";
    string reportName = "Employee List Report";

    public ReportTests(SharedTestHostHolder fixture) : base(fixture) { }

    [Fact]
    public async System.Threading.Tasks.Task LoadFullReport() {
        await WebApiClient.AuthenticateAsync(userName, "");
        string url = CreateRequestUrl(reportName, null, null, null, ExportTarget.Csv);

        string currentData = DateTime.Now.ToString("d", CultureInfo.GetCultureInfo("en-US"));
        string newLine = Environment.NewLine;
        string expectedResult =
            $"Employees,,,,{newLine}" +
            $"{currentData},,,,{newLine}" +
            $"A,Last Name,First Name,Email,Phone{newLine}" +
            $",Aiello,Hewitt,Hewitt_Aiello@example.com,(704) 827-5432{newLine}" +
            $"B,Last Name,First Name,Email,Phone{newLine}" +
            $",Borrmann,Aaron,Aaron_Borrmann@example.com,(760) 156-1374{newLine}" +
            $",Bunch,Abigail,Abigail_Bunch@example.com,(404) 943-6711{newLine}" +
            $",Berntsen,Alberta,Alberta_Berntsen@example.com,(702) 649-5647{newLine}" +
            $",Benson,Anita,Anita_Benson@example.com,(713) 863-8137{newLine}" +
            $",Boyd,Anita,Anita_Boyd@example.com,(303) 376-7233{newLine}" +
            $",Brandt,Arthur,Arthur_Brandt@example.com,(704) 522-7625{newLine}" +
            $",Baker,Carolyn,Carolyn_Baker@example.com,(209) 125-4334{newLine}" +
            $",Bevington,Chandler,Chandler_Bevington@example.com,(817) 141-7655{newLine}" +
            $",Bing,Francine,Francine_Bing@example.com,(720) 861-7141{newLine}" +
            $",Bunkelman,George,George_Bunkelman@example.com,(360) 186-4982{newLine}" +
            $"C,Last Name,First Name,Email,Phone{newLine}" +
            $",Chase,Arvil,Arvil_Chase@example.com,(718) 193-6521{newLine}" +
            $",Chinavare,Barbara,Barbara_Chinavare@example.com,(925) 738-9251{newLine}" +
            $",Cambell,Bruce,Bruce_Cambell@example.com,(417) 166-3268{newLine}" +
            $",Catto,Darlene,Darlene_Catto@example.com,(408) 791-9139{newLine}" +
            $",Crimmins,Dora,Dora_Crimmins@example.com,(860) 826-6458{newLine}" +
            $"D,Last Name,First Name,Email,Phone{newLine}" +
            $",Deville,Andrea,Andrea_Deville@example.com,(303) 718-1654{newLine}" +
            $"F,Last Name,First Name,Email,Phone{newLine}" +
            $",Faircloth,Barbara,Barbara_Faircloth@example.com,(724) 247-3834{newLine}" +
            $"G,Last Name,First Name,Email,Phone{newLine}" +
            $",Geeter,Tony,Tony_Geeter@example.com,(503) 835-2396{newLine}" +
            $"H,Last Name,First Name,Email,Phone{newLine}" +
            $",Hively,Alphonzo,Alphonzo_Hively@example.com,(408) 459-7554{newLine}" +
            $",Hanna,Angelia,Angelia_Hanna@example.com,(509) 169-2345{newLine}" +
            $",Hazel,Anthony,Anthony_Hazel@example.com,(801) 831-7151{newLine}" +
            $",Haneline,Cindy,Cindy_Haneline@example.com,(918) 161-3649{newLine}" +
            $"J,Last Name,First Name,Email,Phone{newLine}" +
            $",Johnson,Alphonso,Alphonso_Johnson@example.com,(816) 767-6243{newLine}" +
            $",Jablonski,Karl,Karl_Jablonski@example.com,(716) 673-5435{newLine}" +
            $"K,Last Name,First Name,Email,Phone{newLine}" +
            $",Korszniak,Andrew,Andrew_Korszniak@example.com,(970) 534-8756{newLine}" +
            $",Keck,Edward,Edward_Keck@example.com,(216) 192-9699{newLine}" +
            $"L,Last Name,First Name,Email,Phone{newLine}" +
            $",Liu,Calvin,Calvin_Liu@example.com,(559) 628-6997{newLine}" +
            $",Limeira,Janete,Janete_Limeira@example.com,(626) 539-3124{newLine}" +
            $"M,Last Name,First Name,Email,Phone{newLine}" +
            $",Melton,Alex,Alex_Melton@example.com,(562) 563-8938{newLine}" +
            $",Mccallum,Angela,Angela_Mccallum@example.com,(860) 722-7357{newLine}" +
            $",Matese,Archie,Archie_Matese@example.com,(253) 782-3416{newLine}" +
            $"N,Last Name,First Name,Email,Phone{newLine}" +
            $",Nolan,Alfred,Alfred_Nolan@example.com,(817) 964-3798{newLine}" +
            $",Nilsen,John,john_nilsen@example.com,(559) 224-4648{newLine}" +
            $"R,Last Name,First Name,Email,Phone{newLine}" +
            $",Ryan,Anita,Anita_Ryan@example.com,(720) 971-3927{newLine}" +
            $",Rounds,Anthony,Anthony_Rounds@example.com,(559) 453-3698{newLine}" +
            $"S,Last Name,First Name,Email,Phone{newLine}" +
            $",Stormo,Amos,Amos_Stormo@example.com,(305) 964-4756{newLine}" +
            $",Stamps,Amy,Amy_Stamps@example.com,(617) 342-3285{newLine}" +
            $",Stender,Charles,Charles_Stender@example.com,(240) 242-4822{newLine}" +
            $",Smodey,Harold,Harold_Smodey@example.com,(785) 963-5491{newLine}" +
            $",Scott,Rachel,Rachel_Scott@example.com,(801) 883-2212{newLine}" +
            $"T,Last Name,First Name,Email,Phone{newLine}" +
            $",Teter,Essie,Essie_Teter@example.com,(214) 126-8555{newLine}" +
            $",Tellitson,Mary,Mary_Tellitson@example.com,(206) 177-7473{newLine}" +
            $"V,Last Name,First Name,Email,Phone{newLine}" +
            $",Vicars,Annie,Annie_Vicars@example.com,(305) 654-4417{newLine}" +
            $"W,Last Name,First Name,Email,Phone{newLine}" +
            $",Walker,Albert,Albert_Walker@example.com,(904) 295-9379{newLine}" +
            $",Walker,Angela,Angela_Walker@example.com,(316) 444-3653{newLine}" +
            $",Waytz,Anthony,Anthony_Waytz@example.com,(704) 272-1178{newLine}" +
            $",Webb,Ernest,Ernest_Webb@example.com,(201) 432-6934{newLine}" +
            $",,,,{newLine}";
        await LoadReportAndCompare(userName, url, expectedResult);
    }

    [Fact]
    public async System.Threading.Tasks.Task LoadReportWithCriteria() {
        await WebApiClient.AuthenticateAsync(userName, "");

        string criteria = CriteriaOperator.FromLambda<Employee>(x => x.FirstName == "Aaron" || x.LastName == "Benson").ToString();
        string url = CreateRequestUrl(reportName, criteria, null, null, ExportTarget.Csv);

        string currentData = DateTime.Now.ToString("d", CultureInfo.GetCultureInfo("en-US"));
        string newLine = Environment.NewLine;
        string expectedResult =
            $"Employees,,,,{newLine}" +
            $"{currentData},,,,{newLine}" +
            $"B,Last Name,First Name,Email,Phone{newLine}" +
            $",Borrmann,Aaron,Aaron_Borrmann@example.com,(760) 156-1374{newLine}" +
            $",Benson,Anita,Anita_Benson@example.com,(713) 863-8137{newLine}" +
            $",,,,{newLine}";
        await LoadReportAndCompare(userName, url, expectedResult);
    }

    private async System.Threading.Tasks.Task LoadReportAndCompare(string userName, string url, string expectedResult) {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "en-US");
        var response = await WebApiClient.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"Request failed for {userName} @ {url} ");

        string loadedReport = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedResult, loadedReport);
    }
    private string CreateRequestUrl(
        string reportName, string criteria = null, string reportParameters = null,
        SortProperty[] sortProperties = null, ExportTarget exportType = ExportTarget.Pdf) {

        string url = $"{DownloadUrl}({reportName})";
        var q = $"fileType={exportType}";

        if(!string.IsNullOrEmpty(criteria)) {
            q += $"&criteria={criteria}";
        }
        if(sortProperties != null && sortProperties.Length > 0) {
            foreach(var sortProperty in sortProperties) {
                q += $"&sortProperty={$"{sortProperty.PropertyName},{sortProperty.Direction}"}";
            }
        }
        if(!string.IsNullOrEmpty(reportParameters)) {
            q += $"&{reportParameters}";
        }

        url += "?" + q;

        return url;
    }

}

