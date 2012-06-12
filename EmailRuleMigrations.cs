using System;
using System.Linq;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;

namespace RegistrationRules
{
    public class EmailRuleMigrations : DataMigrationImpl 
    {
        public EmailRuleMigrations()
        {
        }

        public int Create() 
        {
            this.SchemaBuilder.CreateTable("RoleCollectionRecord", table => table
                                                                                .Column<int>("Id", column => column.PrimaryKey().Identity()));

            this.SchemaBuilder.CreateTable("RoleTargetRecord", table => table
                                                                            .Column<int>("Id", column => column.PrimaryKey().Identity())
                                                                            .Column<string>("RoleName")
                                                                            .Column<int>("RoleCollectionRecord_Id"));

            this.SchemaBuilder.CreateTable("EmailRegistrationMaskRecord", table => table
                                                                                       .Column<int>("Id", column => column.PrimaryKey().Identity())
                                                                                       .Column<string>("Rule"));

            return 1;
        }

        public int UpdateFrom1() 
        {
            this.SchemaBuilder.AlterTable("RoleCollectionRecord", table => table.AddColumn<string>("Name"));

            return 2; 
        }

        public int UpdateFrom2() 
        {
            this.SchemaBuilder.AlterTable("EmailRegistrationMaskRecord", table => { table.AddColumn<string>("Operator"); });

            return 3;
        }

        public int UpdateFrom3() 
        {
            this.SchemaBuilder.DropTable("EmailRegistrationMaskRecord");
            this.SchemaBuilder.CreateTable("RegistrationRuleRecord", table => table
                                                        .Column<int>("Id", column => column.PrimaryKey().Identity())
                                                        .Column<string>("RuleName")
                                                        .Column<string>("Operator")
                                                        .Column<int>("RoleCollectionRecord_Id"));

            return 4;
        }

        public int UpdateFrom4() 
        {
            this.SchemaBuilder.AlterTable("RegistrationRuleRecord", alter =>
            { 
                alter.DropColumn("RoleCollectionRecord_Id");
                alter.AddColumn<int>("RoleTargetRecord_Id");
            });

            return 5;
        }

        public int UpdateFrom5() 
        {
            this.SchemaBuilder.AlterTable("RegistrationRuleRecord", alter =>
            {
                alter.AddColumn<string>("RuleAction", column => column.Unlimited());
            });

            return 6;
        }

        public int UpdateFrom6() 
        {
            this.SchemaBuilder.AlterTable("RegistrationRuleRecord", alter => {
                alter.AddColumn<bool>("CheckUserOnRegistration");
            });

            return 7;
        }

        public int UpdateFrom7() 
        {
            this.SchemaBuilder.AlterTable("RegistrationRuleRecord", alter => {
                alter.AddColumn<bool>("InculusionRule");
            });

            return 8;
        }
    }
}