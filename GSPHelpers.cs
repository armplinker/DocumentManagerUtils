#define SYSEVENTLOGGING
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.nodes;
using gudusoft.gsqlparser.pp.para;
using gudusoft.gsqlparser.pp.para.styleenums;
using gudusoft.gsqlparser.pp.stmtformatter;
using gudusoft.gsqlparser.stmt;
using log4net;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LBIS9
{
    public static class GSPHelpers
    {
        /// <summary>
        /// This is set 1x in the static constructor Helpers
        /// </summary>
        public static bool RunningAsWebApp = false;

        /// <summary>
        ///  Logger property for Helpers object
        /// </summary>
        private static ILog Log { get; }
        //    public GSPHelpers()
        //    {
        //    }

        #region DECLARATIONS

        #endregion
        #region CONSTRUCTOR
        /// <summary>
        /// Static constructor for Helpers
        /// A static constructor is used to initialize any static data, or to perform a particular action that needs performed once only. It is called automatically before the first instance is created or any static members are referenced.
        /// Static constructors have the following properties:
        ///   A static constructor does not take access modifiers or have parameters.
        ///   A static constructor is called automatically to initialize the class before the first instance is created or any static members are referenced.
        ///   A static constructor cannot be called directly.
        ///   The user has no control on when the static constructor is executed in the program.
        ///   A typical use of static constructors is when the class is using a log file and the constructor is used to write entries to this file.
        ///   Static constructors are also useful when creating wrapper classes for unmanaged code, when the constructor can call the LoadLibrary method.
        /// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors
        /// </summary>
        static GSPHelpers()
        {
            // get the logger for the session
            if (HttpContext.Current != null)
            {
                RunningAsWebApp = true;

                if (HttpContext.Current.Session != null)
                {
                    var o = HttpContext.Current.Session["LOGGER"]; //LogManager.GetLogger("LBIS9");
                    if (o != null)
                        Log = (log4net.ILog)o;
                    else if (Globals.Log != null)
                        Log = Globals.Log;
                }
                else // not a web app session
                {
                    Log = LogManager.GetLogger("LBIS9");
                }
            }
            else // not a web app session
            {
                RunningAsWebApp = false;
                Log = LogManager.GetLogger("LBIS9");
            }
        }
        #endregion
        #region METHODS

        /// <summary>
        /// Format a piece of SQL for presentation purposes - NOT guaranteed to return a usable string for execution - use for readability
        /// Results will be TRUNCATED in DEBUG mode
        /// uses SQL_Formatter project to format the SQL https://www.codeproject.com/Articles/1275363/Csharp-Source-for-SQL-Formatting
        /// </summary>
        /// <param name="sqlString">A piece of SQL</param>         
        public static string FormatSqlString(this string sqlString)
        {
            return sqlString.FormatSqlString(cleanString: true, options: null);
        }

        /// <summary>
        /// Format a piece of SQL for presentation purposes - NOT guaranteed to return a usable string for execution - use for readability
        /// Results will be TRUNCATED in DEBUG mode
        /// uses SQL_Formatter project to format the SQL https://www.codeproject.com/Articles/1275363/Csharp-Source-for-SQL-Formatting
        /// </summary>
        /// <param name="sqlString">A piece of SQL</param>
        /// <param name="cleanString">true or false to remove trailing semi, trim</param>
        /// <param name="options"></param>
        public static string FormatSqlString(this string sqlString, bool cleanString, GFmtOpt options)
        {
            var parser = new TGSqlParser(EDbVendor.dbvoracle);
            return sqlString.FormatSqlString(parser, cleanString, options);
        }



        /// <summary>
        /// Format a piece of SQL for presentation purposes - NOT guaranteed to return a usable string for execution - use for readability
        /// Results will be TRUNCATED in DEBUG mode
        /// uses SQL_Formatter project to format the SQL https://www.codeproject.com/Articles/1275363/Csharp-Source-for-SQL-Formatting
        /// </summary>
        /// <param name="sqlString">A piece of SQL</param>
        /// <param name="parser">An instantiated TGSqlParser</param>
        /// <param name="cleanString">true or false to remove trailing semi, trim</param>
        /// <param name="options"></param>
        public static string FormatSqlString(this string sqlString, TGSqlParser parser, bool cleanString, GFmtOpt options)
        {



            // always remove comments which for Oracle always start with -- or /*
            sqlString = (sqlString.Contains("--") || sqlString.Contains("/*") ? sqlString.RemoveCommentsGSP() : sqlString); //            get rid of comments if any...

            if (cleanString)
                sqlString = sqlString.Trim().CleanSQLString();
#if DEBUG
            parser.sqltext = sqlString;

            var retVar = parser.parse();
            var result = "";

            if (retVar == 0)
            {
                options = options ?? GFmtOptFactory.newInstance();
                options.selectColumnlistComma = TLinefeedsCommaOption.LfBeforeComma;
                options.selectFromclauseJoinOnInNewline = true;
                options.removeComment = true;
                result = FormatterFactory.pp(parser, options);

            }

            else
            {
                result = string.Empty;

            }

            return result;
#else
    return sqlString;
#endif
        }

        /// <summary>
        /// For a given Sql string, check its syntax against the vendor standard (wrapper)
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public static bool CheckSyntax(this string sqlString)
        {
            return CheckSyntax(sqlString: sqlString, null);
        }
        /// <summary>
        /// For a given Sql string, check its syntax against the vendor standard (wrapper)
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static bool CheckSyntax(this string sqlString, bool? throwError)
        {
            return CheckSyntax(sqlString: sqlString, parser: null, throwError: throwError);
        }

        /// <summary>
        /// For a given Sql string, check its syntax against the vendor standard
        /// </summary>
        /// <param name="sqlString"></param>
        /// <param name="parser"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static bool CheckSyntax(this string sqlString, TGSqlParser parser, bool? throwError = false)
        {


            if (string.IsNullOrEmpty(sqlString))
                throw new ArgumentNullException($@"CheckSyntax: The sql string argument { nameof(sqlString)} is null or empty.", nameof(sqlString));

#if DEBUG
            var errs = 0;
            try
            {

                parser = parser ?? new TGSqlParser(EDbVendor.dbvoracle);
                parser.sqltext = sqlString;
                errs = parser.parse();

                if (errs > 0)
                {

                    var msg = new StringBuilder().AppendLine($"Input SQL had {errs} errors");

                    foreach (var parserErrMsg in parser.SyntaxErrors.Select(err =>
                        $@"Err: {err.errorno} - Line: {err.lineNo} - Column: {err.columnNo} - TokenText: {err.tokentext} - Hint: {err.hint ?? string.Empty}")
                    )
                    {
                        msg.AppendLine(parserErrMsg);
                    }
                    throw new Exception(msg.ToString());
                }

            }
            catch (Exception e)
            {

                e?.Log($@"Syntax errors detected {errs} : {parser?.Errormessage} {Environment.NewLine} Details: {e.GatherExceptionData()} {Environment.NewLine}{sqlString}");
                if (throwError != null && throwError == true)
                    throw;
            }
            return errs == 0;
#else
            return true;
#endif

        }

        /// <summary>
        /// Format a piece of SQL for presentation purposes - NOT guaranteed to return a usable string for execution - use for readability
        /// Results will be TRUNCATED in DEBUG mode
        /// uses SQL_Formatter project to format the SQL https://www.codeproject.com/Articles/1275363/Csharp-Source-for-SQL-Formatting
        /// </summary>
        /// <param name="sqlString">A piece of SQL</param>
        /// <param name="parser">An instantiated TGSqlParser</param>
        /// <param name="options"></param>
        public static string SimplifySqlString(this string sqlString, TGSqlParser parser, GFmtOpt options = null)
        {
            /*
             * 
        static void outputPlainFormat(TGSqlParser parser)
        {
            int ret = parser.parse();
            if (ret == 0)
            {
                GFmtOpt option = GFmtOptFactory.newInstance();
                string result = FormatterFactory.pp(parser, option);
                Console.WriteLine(result);
            }
            else
            {
                Console.WriteLine(parser.Errormessage);
            }
        }

             */

            parser.sqltext = sqlString;
            var retVar = parser.parse();
            var result = "";

            if (retVar == 0)
            {
                options = options ?? GFmtOptFactory.newInstance();

                options.selectColumnlistComma = TLinefeedsCommaOption.LfBeforeComma;
                options.selectFromclauseJoinOnInNewline = true;
                options.removeComment = true;

                result = FormatterFactory.pp(parser, options);
#if DEBUG
                //Console.WriteLine(result);
#endif
            }

            else
            {
                result = string.Empty;
#if DEBUG
                // Console.WriteLine(parser.Errormessage);
#endif

            }

            return result;

        }

        /// <summary>
        /// Find outermost ORDER BY in a sql statement, optionally throw an error if a problem is encountered such as parsing failure
        /// </summary>
        /// <param name="srcStr"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static string FindOrderByGSP(this string srcStr, bool? throwError = false)
        {
            try
            {
                var sqlParser = new TGSqlParser(EDbVendor.dbvoracle) { sqltext = srcStr };
                var ret = sqlParser.parse(); // 0 on success, any positive # indicates errors

                // gotta parse or fail this
                if (ret > 0)
                {
                    if (throwError == true)
                        throw new ArgumentException(
                            $@"SQLstring {srcStr} has {sqlParser.ErrorCount} errors: {sqlParser.Errormessage}");
                    return string.Empty;
                }

                TSelectSqlStatement select = (TSelectSqlStatement)sqlParser.sqlstatements[0];

                if (@select == null) return string.Empty;

                TOrderBy orderBy = @select?.OrderbyClause;

                Console.WriteLine($"Order By: {orderBy}");
                foreach (var item in orderBy.Items)
                    Console.WriteLine($"OrderBy Item: {item}");
                return orderBy.ToString();
            }
            catch (Exception ex)
            {
                ex?.Log(ex.GatherExceptionData());
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcSql"></param>
        /// <param name="orderByCondition"></param>
        /// <param name="sortFlag">1 - sort asc, 2 -- desc, 0 -- none ???</param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static string AddOrderByClauseGSP(this string srcSql, string orderByCondition, int sortFlag = 1,
            bool? throwError = false)
        {
            try
            {
                if (string.IsNullOrEmpty(orderByCondition))
                {
                    if ((bool)throwError)
                        throw new ArgumentNullException(@"orderByCondition argument cannot be null");
                    return string.Empty;
                }

                if (!(sortFlag == 0 || sortFlag == 1 || sortFlag == 2))
                {
                    if ((bool)throwError)
                        throw new ArgumentNullException(@"sortFlag argument must be 0, 1, or 2");
                    return string.Empty;
                }

                var parser = new TGSqlParser(EDbVendor.dbvoracle) { sqltext = srcSql }; //"SELECT * FROM TABLE_X";
                var ret = parser.parse();

                if (ret != 0)
                {
                    if ((bool)throwError)
                        throw new InvalidOperationException(@"Parse failed for sql");
                    return string.Empty;
                }

                TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);

                TOrderBy orderBy = new TOrderBy();
                select.OrderbyClause = orderBy;
                TOrderByItem orderByItem = new TOrderByItem();
                orderBy.Items.addOrderByItem(orderByItem);
                orderByItem.SortKey = parser.parseExpression(orderByCondition);
                orderByItem.SortOrder =
                    (sortFlag == 1 ? ESortType.asc :
                        (sortFlag == 2) ? ESortType.desc : ESortType.none); // doubtfull sorttype.none is useful

                return select.ToScript();
            }
            catch (ArgumentException ae)
            {
                ae?.Log(ae.GatherExceptionData());
                throw;
            }

            catch (Exception e)
            {
                e?.Log(e.GatherExceptionData());
                throw;
            }
        }

        public static string ReplaceVeryFirstWhere(this string text, string search, string replace = "", StringComparison compare = StringComparison.OrdinalIgnoreCase)
        {
            int pos = text.Trim().IndexOf(search.Trim(), compare);
            if (pos != 0) // we only want to get rid of the FIRST one we find, not any buried deeper in the WHERE clause
            {
                return text;
            }
            // pos == 0
            return text.Substring(0, pos) + replace + text.Substring(search.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcStr"></param>
        /// <param name="modString"></param>
        /// <param name="regexPattern">Pass empty string to skip regex check to avoid adding where clause twice</param>
        /// <param name="regexOpts"></param>
        /// <param name="andOrFlg">0 = AND, 1 = OR, other values either throw an exception or set the flag to 0</param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static string ModifyWhereClauseGSP(this string srcStr, string modString, string regexPattern = "",
            RegexOptions regexOpts = RegexOptions.None, int andOrFlg = 0, bool? throwError = false)
        {
            try
            {
                // verify that a modstring was passed 
                if (string.IsNullOrEmpty(modString.Trim()))
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException($"required ModString is an empty string!");
                    return string.Empty;
                }

                foreach (var keyword in "WHERE;AND;OR;".Split(';'))
                    // CUTOFF LEADING WHERE, AND, or OR
                    modString = modString.Trim().ReplaceVeryFirstWhere(keyword, string.Empty);

                // verify and or flag
                if (andOrFlg != 1 && andOrFlg != 0)
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException($"AndOrFlg is not 1 or 0");
                    andOrFlg = 0;
                }



                var sqlParser = new TGSqlParser(EDbVendor.dbvoracle) { sqltext = srcStr };
                var ret = sqlParser.parse(); // 0 on success, any positive # indicates errors
                var modResult = string.Empty;

                // gotta parse or fail this
                if (ret > 0)
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException(
                            $@"SQL string {srcStr} has {sqlParser.ErrorCount} errors: {sqlParser.Errormessage}");
                    return string.Empty;
                }

                var sqlStatement = sqlParser.sqlstatements[0];

                if (sqlStatement == null) return string.Empty;



                TExpression expression1 = sqlParser.parseExpression(modString);

                if (expression1 == null)
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException(
                            $@"The where clause condition {modString} is invalid and failed to parse - expression to add is null");
                    return string.Empty; // failed..
                }

                if (sqlStatement.WhereClause == null) // none at all
                {
                    TWhereClause whereClause = new TWhereClause();
                    sqlStatement.WhereClause = whereClause;
                    whereClause.Condition = expression1;
                }
                else // modify existing
                {
                    TExpression expression2 = new TExpression
                    {
                        ExpressionType = (andOrFlg == 0)
                            ? EExpressionType.logical_and_t
                            : EExpressionType.logical_or_t
                    };
                    // 0 or AND is the default
                    TExpression parensExpr = new TExpression
                    {
                        ExpressionType = EExpressionType.parenthesis_t,
                        LeftOperand = sqlStatement.WhereClause.Condition
                    };

                    // if it already contains this token modString
                    if (!string.IsNullOrEmpty(regexPattern))
                        if (new Regex(regexPattern, regexOpts).IsMatch(sqlStatement.WhereClause.ToString()))
                            return srcStr;

                    expression2.LeftOperand = parensExpr;
                    expression2.RightOperand = expression1;
                    sqlStatement.WhereClause.Condition = expression2;
                }

                return sqlStatement.ToScript();

            }
            catch (InvalidCastException ie)
            {
                ie?.Log(ie.GatherExceptionData());
                throw;
            }
            catch (Exception ex)
            {
                ex?.Log(ex.GatherExceptionData());
                throw;
            }
        }

        /// <summary>
        /// Find the outermost WHERE clause in a sql statement, optionally throw an error if a problem is encountered such as parsing failure
        /// </summary>
        /// <param name="srcStr"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static string FindWhereClauseGSP(this string srcStr, bool? throwError = false)
        {
            try
            {
                var sqlParser = new TGSqlParser(EDbVendor.dbvoracle) { sqltext = srcStr };
                var ret = sqlParser.parse(); // 0 on success, any positive # indicates errors

                // gotta parse or fail this
                if (ret > 0)
                {
                    if (throwError == true)
                        throw new ArgumentException(
                            $@"SQLstring {srcStr} has {sqlParser.ErrorCount} errors: {sqlParser.Errormessage}");
                    return string.Empty;
                }

                TSelectSqlStatement select = (TSelectSqlStatement)sqlParser.sqlstatements[0];

                if (@select == null) return string.Empty;

                TWhereClause whereClause = @select?.WhereClause;

                //Console.WriteLine($"WHere Clause: {whereClause}");

                return whereClause.ToString();
            }
            catch (ArgumentException ae)
            {
                ae?.Log(ae.GatherExceptionData());
                throw;
            }
            catch (Exception ex)
            {
                ex?.Log(ex.GatherExceptionData());
                throw;
            }
        }

        /// <summary>
        /// Extension method to get the position of a TResultColumn a.n in the result set, or -1 if not found. -2 on failure
        /// </summary>
        /// <param name="srcStr"></param>
        /// <param name="findColStr"></param>
        /// <param name="colAlias"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="parser"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static int GetTResultColumnPosition(this string srcStr, string findColStr, string colAlias = "", bool? ignoreCase = true,
            TGSqlParser parser = null, bool? throwError = false)
        {
            try
            {
                parser = parser ?? new TGSqlParser(EDbVendor.dbvoracle);
                parser.sqltext = srcStr;
                var ret = parser.parse();

                if (ret > 0)
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException(
                            $@"Sql {srcStr} string failed parsing");
                    Globals.Log?.Error($@"Sql {srcStr} string failed parsing");
                    return -2; // means failed!
                }

                TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
                TResultColumnList columns = select.ResultColumnList;
                TResultColumn findResultColumn = new TResultColumn { Expr = parser.parseExpression(findColStr) };

                if (!string.IsNullOrEmpty(colAlias))
                {
                    TAliasClause aliasClause = new TAliasClause
                    {
                        AliasName = parser.parseObjectName(colAlias),
                        HasAs = true
                    };

                    findResultColumn.AliasClause = aliasClause;
                }

                for (var i = 0; i < columns.Count; i++)
                {
                    var tcol = columns.getResultColumn(i);
                    if (string.Equals(findResultColumn.Expr.String, tcol.Expr.String, (ignoreCase == true) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) && (string.IsNullOrEmpty(colAlias) || string.Equals(findResultColumn.AliasClause.AliasName.String, tcol.AliasClause.AliasName.String, (ignoreCase == true) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                    {
                        return i;
                    }
                }
                return -1; // means not found
            }
            catch (Exception e)
            {
                e?.Log(e.GatherExceptionData());
                throw;
            }
        }

        // add a column to the result set
        // typically to add b.BRIDGE_GD if missing from the SQL
        /*
         *  parser.sqltext = "SELECT A as A_Alias, B AS B_Alias FROM TABLE_X";
            Assert.IsTrue(parser.parse() == 0);
            TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
            TResultColumnList columns = select.ResultColumnList;

            TResultColumn resultColumn = new TResultColumn();
            resultColumn.Expr = parser.parseExpression("d");
            columns.addResultColumn(resultColumn);
            TAliasClause aliasClause = new TAliasClause();
            aliasClause.AliasName = parser.parseObjectName("d_alias");
            aliasClause.HasAs = true;
            resultColumn.AliasClause = aliasClause;
         */
        /// <summary>
        /// Add a column to the result set on-the-fly
        /// </summary>
        /// <param name="srcStr"></param>
        /// <param name="addColStr"></param>
        /// <param name="parser"></param>
        /// <param name="colAlias"></param>
        /// <param name="throwError"></param>
        /// <returns></returns>
        public static string AddColumnToResultSet(this string srcStr, string addColStr, string colAlias = "",
            TGSqlParser parser = null,
            bool? throwError = false)
        {
            try
            {
                parser = parser ?? new TGSqlParser(EDbVendor.dbvoracle);
                parser.sqltext = srcStr;
                var ret = parser.parse();

                if (ret > 0)
                {
                    if (throwError != null && (bool)throwError)
                        throw new ArgumentException(
                            $@"Sql {srcStr} string failed parsing");
                    Globals.Log?.Error($@"Sql {srcStr} string failed parsing");
                    return string.Empty;
                }

                TSelectSqlStatement select = (TSelectSqlStatement)parser.sqlstatements.get(0);
                TResultColumnList columns = select.ResultColumnList;

                TResultColumn newResultColumn = new TResultColumn { Expr = parser.parseExpression(addColStr) };


                var exists = false;
                for (var i = 0; i < columns.Count; i++)
                {
                    var tcol = columns.getResultColumn(i);
                    if (newResultColumn.Expr.String == tcol.Expr.String && (string.IsNullOrEmpty(colAlias) || newResultColumn.Expr.ExprAlias == tcol.Expr.ExprAlias))
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) return srcStr;  // do nothing
                                            // does not, add


                columns.addResultColumn(newResultColumn);

                if (!string.IsNullOrEmpty(colAlias))
                {
                    // add alias
                    TAliasClause aliasClause = new TAliasClause
                    {
                        AliasName = parser.parseObjectName(colAlias),
                        HasAs = true
                    };

                    newResultColumn.AliasClause = aliasClause;
                }
                srcStr = select.ToScript();
                return srcStr;
            }
            catch (ArgumentException ae)
            {
                ae?.Log(ae.GatherExceptionData());
                throw;
            }

            catch (Exception e)
            {
                e?.Log(e.GatherExceptionData());
                throw;
            }
        }

        public static string RemoveCommentsGSP(this string srcStr, [CallerMemberName] string caller = "")
        {
            try
            {
                var ctx = HttpContext.Current?.Session?.SessionID ?? Guid.NewGuid().ToString();

                GFmtOpt option =
                    GFmtOptFactory.newInstance(ctx + "." + caller + "." +
                                               new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name);

                option.removeComment = true;

                TGSqlParser sqlparser = new TGSqlParser(EDbVendor.dbvoracle) { sqltext = srcStr }; // was referring to dbvmssql !! - corrected 20200812
                //"select department_id,\n" + "       min( salary ) -- single line comment \n" +
                //"from   employees \n" + "group  by department_id";

                sqlparser.parse();

                return FormatterFactory.pp(sqlparser, option);

            }
            catch (ArgumentException ae)
            {
                ae?.Log(ae.GatherExceptionData());
                throw;
            }

            catch (Exception e)
            {
                e?.Log(e.GatherExceptionData());
                throw;
            }
        }

        /// <summary>
        /// Dump errors to the console and debug log
        /// </summary>
        /// <param name="parser">an active parser with errors....</param>
        /// <param name="logToConsole">in conjunction with the RUnningAsWebApp property will log the messages to the console</param>
        /// <returns>Void</returns>
        public static void DumpParserErrorsGSP(this TGSqlParser parser, bool? logToConsole = false)
        {
            try
            {
                Log.Debug(parser.Errormessage);

                if (logToConsole != null && logToConsole == true && !RunningAsWebApp)
                    Console.WriteLine(parser.Errormessage);

                foreach (var parserErrMsg in parser.SyntaxErrors.Select(err => $@"Err: {err.errorno} - Line: {err.lineNo} - Column: {err.columnNo} - TokenText: {err.tokentext} - Hint: {err.hint ?? string.Empty}"))
                {
                    Log.Debug(parserErrMsg);

                    if (logToConsole != null && logToConsole == true && !RunningAsWebApp)
                        Console.WriteLine(parserErrMsg);
                }
            }
            catch (Exception e)
            {
                e?.Log(e.GatherExceptionData());
                throw;
            }

        }

        #endregion
    }
}