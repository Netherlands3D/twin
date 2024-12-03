using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public static class ATMUtilities
    {
        public static int[] years = { 1802, 1853, 1870, 1876, 1909, 1920, 1943 };

        public static int RoundDownYear(int inputYear)
        {
            // Find the largest year in the array that is less than or equal to inputYear
            int result = years[0];
            foreach (var year in years)
            {
                if (year <= inputYear)
                {
                    result = year;
                }
                else
                {
                    break; // Stop checking once we've exceeded the inputYear
                }
            }
        
            return result;
        }
    }
}
