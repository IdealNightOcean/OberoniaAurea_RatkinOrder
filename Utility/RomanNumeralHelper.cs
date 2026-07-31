namespace OberoniaAurea.RatkinOrder.Utility;

using System;
using System.Collections.Generic;

public static class RomanNumeralHelper
{
    /// <summary>
    /// 预缓存 1~100 全部结果，只在程序启动静态构造生成一次
    /// </summary>
    private static readonly string[] numCaches;
    private static readonly Dictionary<int, string> customNumCaches;

    private static readonly (int val, string sym)[] symbols =
    [
        (100, "C"),
        (90, "XC"),
        (50, "L"),
        (40, "XL"),
        (10, "X"),
        (9, "IX"),
        (5, "V"),
        (4, "IV"),
        (1, "I")
    ];

    static RomanNumeralHelper()
    {
        customNumCaches = new(16);
        numCaches = new string[101];
        for (int i = 1; i <= 100; i++)
        {
            numCaches[i] = ToRomanRaw(i);
        }
    }

    /// <summary>
    /// 将数字转为罗马数字
    /// </summary>
    /// <param name="number">数值</param>
    /// <param name="addToCache">超过100时，是否存入自定义缓存</param>
    /// <returns>罗马数字文本</returns>
    /// <exception cref="ArgumentOutOfRangeException">超出标准罗马数字范围 1~3999</exception>
    public static string ToRoman(int number, bool addToCache = false)
    {
        if (number < 1 || number > 3999)
            throw new ArgumentOutOfRangeException(nameof(number), $"Roman numeral only supports range 1 ~ 3999, input value: {number}");

        if (number <= 100)
            return numCaches[number];

        if (customNumCaches.TryGetValue(number, out string result))
            return result;

        result = ToRomanRaw(number);
        if (addToCache)
            customNumCaches[number] = result;

        return result;
    }

    private static string ToRomanRaw(int num)
    {
        string romanNum = string.Empty;
        foreach ((int val, string sym) in symbols)
        {
            while (num >= val)
            {
                romanNum += sym;
                num -= val;
            }
            if (num <= 0) break;
        }
        return romanNum;
    }
}