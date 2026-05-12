using System;
using Microsoft.Data.SqlClient;

namespace Demos.Data {
    public static class DemoDbEngineDetectorHelper {
        public static string DBServerIsNotAccessibleMessage = "This XAF Demo application failed to access your SQL database server.";
        public static string AlternativeName = "EF Core InMemoryDatabase";
        public static string InMemoryDatabaseUsageMessage = "This may cause performance issues. All data modifications will be lost when you close the application.";
        private static bool? isSqlAccessible;
        private const string testConnectionString = "Integrated Security=SSPI;Data Source=(localdb)\\mssqllocaldb;";
        public static bool IsSqlServerAccessible() {
            if(CheckIsSqlServerAccessible()) {
                return true;
            }
            else {
                FillUseSQLAlternativeInfo(DemoDbEngineDetectorHelper.DBServerIsNotAccessibleMessage);
                return false;
            }
        }
        private static bool CheckIsSqlServerAccessible() {
            if(isSqlAccessible.HasValue) {
                return isSqlAccessible.Value;
            }
            bool result = true;
            try {
                using(SqlConnection sqlConnection1 = new SqlConnection(testConnectionString))
                using(SqlConnection sqlConnection2 = new SqlConnection(testConnectionString)) { // check single connection limit
                    sqlConnection1.Open();
                    sqlConnection2.Open();
                }
            }
            catch {
                result = false;
            }
            isSqlAccessible = result;
            return result;
        }
        private static void FillUseSQLAlternativeInfo(string sqlIssue) {
            UseSQLAlternativeInfoSingleton.Instance.FillFields(sqlIssue, AlternativeName, InMemoryDatabaseUsageMessage);
        }
    }
}
