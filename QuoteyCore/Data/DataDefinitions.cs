using System;
using System.Collections.Generic;
using System.Text;

namespace QuoteyCore.Data
{
    public class DataDefinitions
    {
        // Quotes table
        public static string QUOTES_TABLE = "quotey_quote";
        public static string QUOTES_TABLE_HASH_KEY = "Id";

        // Quotes proposals table
        public static string QUOTES_PROPOSAL_TABLE = "quotey_quote_proposal";
        public static string QUOTES_PROPOSAL_TABLE_HASH_KEY = "DateCreated";
        public static string QUOTES_PROPOSAL_TABLE_SORT_KEY = "ReferenceId";
        public static string QUOTES_PROPOSAL_TABLE_TTL = "TTL";
        public static int QUOTES_PROPOSAL_TABLE_TTL_VALUE_HOURS = 27;
    }
}
