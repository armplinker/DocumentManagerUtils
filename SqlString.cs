using System;

public class SqlString
{
    private string sqlString;

    /// <summary>
    /// reference a static method to clean me...
    /// </summary>
    /// <param name="cleanTrailingSemiColon"></param>
    /// <returns>cleaned string</returns>
    public string CleanSqlString(bool cleanTrailingSemiColon = true)
    {
        return sqlString.CleanSqlString(cleanTrailingSemiColon);
    }

    /// <summary>
    /// reference a static method to remove any trailing semicolons (0:N)
    /// </summary>
    /// <param name="throwError"></param>
    /// <returns></returns>
    public string CleanTrailingSemicolon(bool throwError = false)
    {
        return sqlString.CleanTrailingSemicolon(throwError);
    }

    /// <summary>
    /// reference a static method to check my syntax
    /// </summary>
    /// <param name="throwError"></param>
    /// <returns></returns>
    public bool CheckSyntax(bool throwError = false)
    {
        return sqlString.CheckSyntax(throwError);
    }


}

