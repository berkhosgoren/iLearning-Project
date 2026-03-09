using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
               CREATE INDEX IF NOT EXISTS ""IX_Users_Name_Trgm""
               ON ""Users""
               USING GIN (""Name"" gin_trgm_ops);");

            migrationBuilder.Sql(@"
               CREATE INDEX IF NOT EXISTS ""IX_Users_Email_Trgm""
               ON ""Users""
               USING GIN (""Email"" gin_trgm_ops);");

            migrationBuilder.Sql(@"
               CREATE INDEX IF NOT EXISTS ""IX_Inventories_SearchVector_GIN""
               ON ""Inventories""
               USING GIN (""SearchVector"");");

            migrationBuilder.Sql(@"
               CREATE INDEX IF NOT EXISTS ""IX_Users_Name_Lower""
               ON ""Users"" ((lower(""Name"")));");

            migrationBuilder.Sql(@"
               CREATE INDEX IF NOT EXISTS ""IX_Users_Email_Lower""
               ON ""Users"" ((lower(""Email"")));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Email_Lower"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Name_Lower"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Inventories_SearchVector_GIN"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Email_Trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Name_Trgm"";");
        }
    }
}
